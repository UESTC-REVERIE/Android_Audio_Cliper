using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.IO;
using UnityEngine.Android;
using System;
/// <summary>
/// 音频文件的处理以及初始化
/// <para>@author reverie</para>
/// </summary>
public class output : MonoBehaviour
{   
    private IEnumerator ie;//数据请求协程
    [SerializeField] public UI ui = new UI();//UI界面
    [SerializeField] private AudioClip originalClip;//储存初始音频
    [SerializeField] private bool PC_Debug;//Debug选项
    [SerializeField] private Listener listener = new Listener();//开始结束点的监听器
    [Header("删除原有文件选项")] [SerializeField] public Toggle toggle;
    [Header("音频地址下拉菜单")] [SerializeField] public Dropdown dropdown;
    [Header("保存地址下拉菜单")] [SerializeField] public Dropdown saveDropdown;

    [Header("开始时间滑动条")] [SerializeField] public Slider sliderBegin;
    [Header("开始时间输入")] [SerializeField] public InputField inputBegin;
    [Header("结束时间输入")] [SerializeField] public InputField inputEnd;
    [Header("结束时间滑动条")] [SerializeField] public Slider sliderEnd;

    [Header("保存的文件名输入")] [SerializeField] public InputField inputSaveName;


    //private string path = "jar:file:///storage/emulated/0/Music/";//发送file请求路径
    [SerializeField] public AudioSource audioSource;//Unity音频播放源
    
    [SerializeField] private string saveName = "newAudio";
    private string curPath = "/storage/emulated/0/";//当前路径
    private string savePath = "/storage/emulated/0/";//保存地址
    Dictionary<string,string> pastPath = new Dictionary<string,string>();//字典模拟链表形式，存储上级路径
    Dictionary<string,string> savePastPath = new Dictionary<string,string>();
    private List<string> curOptionNameList => GetFilesName(curPath);//保存当前Dropdown选项

    private List<string> saveOptionNameList => GetFilesName(savePath);//保存当前saveDropdown选项
    private void Start() { 

        if(!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead)){
            Permission.RequestUserPermission(Permission.ExternalStorageRead);
        }

        if(!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite)){
            Permission.RequestUserPermission(Permission.ExternalStorageWrite);
        }

        InitOptions();
        
        //监听UI值变化(触发事件中增加委托)
        inputBegin.onEndEdit.AddListener((string str)=>{
            listener.beginTime = float.Parse(str);
        });
        inputEnd.onEndEdit.AddListener((string str)=>{
            listener.endTime = float.Parse(str);
        });
        sliderBegin.onValueChanged.AddListener((float val)=>{
            listener.beginTime = val;
        });
        sliderEnd.onValueChanged.AddListener((float val)=>{
            listener.endTime = val;
        });

        listener.onBeginValueChanged += (float val) =>{
            inputBegin.text = val.ToString("f2");
            sliderBegin.value = val;
        };


        listener.onEndValueChanged += (float val) =>{
            inputEnd.text = val.ToString("f2");
            sliderEnd.value = val;
        };


        inputSaveName.onEndEdit.AddListener((string str)=>{
            saveName = str;
        });

        pastPath[curPath] = curPath;
        savePastPath[savePath] = savePath;
    }
    /// <summary>
    /// 工具函数：抛出异常
    /// </summary>
    /// <param name="warnStr">警告语</param>
    private void errorWarning(string warnStr){
        ui.inputField.text = warnStr;
        ui.fade(ui.errorWin);
    }

    /// <summary>
    /// 工具函数：设置起始结束时间值
    /// </summary>
    /// <param name="_beginTime">起始时间</param>
    /// <param name="_endTime">结束时间</param>
    private void setTime(float _beginTime,float _endTime){
        sliderBegin.maxValue = _endTime - _beginTime;
        sliderEnd.maxValue = _endTime - _beginTime;
        listener.beginTime = _beginTime;
        listener.endTime = _endTime;
    }


    /// <summary>
    /// 获取当前路径下的文件及文件夹名
    /// </summary>
    /// <returns>文件名列表</returns>
    private List<string> GetFilesName(string _curPath){
        List<string> _nameList = new List<string>();
        if(Directory.Exists(_curPath)){
            DirectoryInfo dir = new DirectoryInfo(_curPath);

            FileInfo[] files = dir.GetFiles("*",SearchOption.TopDirectoryOnly);
            DirectoryInfo[] childDirs = dir.GetDirectories("*",SearchOption.TopDirectoryOnly); 

            foreach(var file in files){
                _nameList.Add(file.Name);
            }

            foreach(var childDir in childDirs){
                if(childDir.Name[0]=='.') continue;//排除隐藏文件
                _nameList.Add(childDir.Name);
            }

        }else{
            Debug.Log("PATH NOT EXIST: " + _curPath);

        }

        return _nameList;
    }
    private void InitOptions(){
        dropdown.ClearOptions();
        saveDropdown.ClearOptions();

        dropdown.options.Add(new Dropdown.OptionData("请选择音频文件"));
        dropdown.options.Add(new Dropdown.OptionData(".."));

        saveDropdown.options.Add(new Dropdown.OptionData("请选择保存目录"));
        saveDropdown.options.Add(new Dropdown.OptionData(".."));

        saveDropdown.GetComponentInChildren<Text>().text = "请选择保存目录";
        List<string> _nameList = curOptionNameList;
        foreach(var optionName in _nameList){
            Dropdown.OptionData option = new Dropdown.OptionData(optionName);
            dropdown.options.Add(option);
            saveDropdown.options.Add(option);
        }

    }
    
    /// <summary>
    /// 刷新DropDown选项
    /// <para>设置在选项改变时</para>
    /// </summary>
    public void FlushOption(Dropdown _dropdown){
        int _value = _dropdown.value;
        string _curDir = _dropdown.options[_value].text;
        switch(_value){
            case 0://.
                return;
            case 1://..
                if(_dropdown.name == "Dropdown")curPath = pastPath[curPath];
                if(_dropdown.name == "saveDropdown")savePath = savePastPath[savePath];
                break;
            default:
                string _pastPath = "";
                if(_dropdown.name == "Dropdown") _pastPath = curPath;
                if(_dropdown.name == "saveDropdown") _pastPath = savePath;
                //错误选择警告，待制作渐变弹窗
                if(_curDir.EndsWith(".apk")||_curDir.EndsWith(".jpg")||_curDir.EndsWith(".png")||_curDir.EndsWith(".txt")){//非音频文件
                    if(_dropdown.name == "Dropdown") errorWarning("请选择正确音频类型");
                    if(_dropdown.name == "saveDropdown") errorWarning("请选择目录");
                    break;
                }
                if(_curDir.EndsWith(".flac")){//不支持的音频文件
                    errorWarning("抱歉，暂不支持此格式");
                    break;
                }
                if(_curDir.EndsWith(".mp3")||_curDir.EndsWith(".wav")||_curDir.EndsWith(".ogg")){
                    if(_dropdown.name == "saveDropdown") {
                        errorWarning("请选择目录");
                        break;
                    }
                    curPath += _curDir;
                    LoadAudio(curPath);
                }
                else{
                    if(_dropdown.name == "Dropdown") curPath += _curDir+"/";
                    if(_dropdown.name == "saveDropdown")savePath += _curDir+"/";
                }
                if(_dropdown.name == "Dropdown") pastPath[curPath] = _pastPath;
                if(_dropdown.name == "saveDropdown") savePastPath[savePath] = _pastPath; 
                break;
        }
        
        _dropdown.ClearOptions();

        _dropdown.options.Add(new Dropdown.OptionData("."));
        _dropdown.options.Add(new Dropdown.OptionData(".."));

        Text label  = _dropdown.GetComponentInChildren<Text>();
    
        if(label != null){
            if(_dropdown.name == "Dropdown")
                label.text = curPath.Replace("/storage/emulated/0/","/");
            if(_dropdown.name == "saveDropdown")
                label.text = savePath.Replace("/storage/emulated/0/","/");
        }
        else Debug.Log("NOT FOUND LABEL!");

        List<string> _nameList = new List<string>();
        if(_dropdown.name == "Dropdown") _nameList = curOptionNameList;
        if(_dropdown.name == "saveDropdown") _nameList = saveOptionNameList;  //最后一次更新表单
        //_dropdown.options = _nameList.ConvertAll((n)=>new Dropdown.OptionData(n));
        foreach(var optionName in _nameList){
            Dropdown.OptionData option = new Dropdown.OptionData(optionName);
            _dropdown.options.Add(option);
        }
        
    }

    /// <summary>
    /// 实现音频剪辑
    /// Unity中样本数指单个通道采样个数，等于采样率x时间，与真正的采样个数不同（采样率x时间x通道数）
    /// </summary>
    public void AudioCliper(bool isSave){
        if(listener.endTime < listener.beginTime){
            errorWarning("请选择正确剪切时间");
            return;
        }
        if(originalClip == null){
            errorWarning("请选择音频");
            return;
        }
        AudioClip Clip = originalClip;
        // float L_clip = Clip.length;
        // Debug.Log(Clip.length+"\n");//clip时长
        // Debug.Log(Clip.frequency+"\n");
        // Debug.Log(Clip.channels+"\n");
        
        // Debug.Log(Clip.samples);//采样点个数
        float[] samples = new float[Clip.samples * Clip.channels];//Unity以32位为采样位数，支持多声道

        Clip.GetData(samples,0);

        int begin_samples = (int) (listener.beginTime * Clip.frequency * Clip.channels);
        int len_samples = (int) ((listener.endTime - listener.beginTime) * Clip.frequency * Clip.channels);

        float[] new_samples = new float[len_samples];
        // Debug.Log("samples.Length:" + samples.Length);
        // Debug.Log("new_samples.Length:" + new_samples.Length);
        Array.Copy(samples,begin_samples,new_samples,0,len_samples);
        AudioClip newClip = AudioClip.Create("newClip",len_samples / Clip.channels,Clip.channels,Clip.frequency,false);

        newClip.SetData(new_samples,0);

        audioSource.clip = newClip;
        //Debug.Log(audioSource.clip.length);
        if(isSave) Save(saveName,newClip);
        audioSource.Play();

    }
    
  #region 文件保存


    /// <summary>
    /// 录音文件保存
    /// </summary>
    const int HEADER_SIZE = 44;
    void Save(string fileName, AudioClip clip)
    {
        if(fileName.EndsWith(".flac")||fileName.EndsWith(".mp3")||fileName.EndsWith(".ogg")){
            errorWarning("抱歉，现仅支持储存为wav格式");
            return;
        }
        if (!fileName.ToLower().EndsWith(".wav"))
        {
            fileName += ".wav";
        }
        string filePath="";
        if(PC_Debug) filePath = Path.Combine(Application.dataPath, fileName);
        else filePath = savePath + fileName;
        if (File.Exists(filePath))
        {
            if(toggle.isOn){
                File.Delete(filePath);
            }
            else{
                errorWarning("文件已存在");
                return;
            }
        }
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        Debug.Log(filePath);
        //创建头
        FileStream fs = CreateEmpty(filePath);
        //写语音数据
        ConvertAndWrite(fs, clip);
        //重写真正的文件头
        WriteHeader(fs, clip);
        fs.Flush();
        fs.Close();
    }
    /// <summary>
    /// 创建头
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    FileStream CreateEmpty(string filePath)
    {
        var fileStream = new FileStream(filePath, FileMode.Create);
        byte emptyByte = new byte();
        for (int i = 0; i < HEADER_SIZE; i++)
        {
            fileStream.WriteByte(emptyByte);
        }
        return fileStream;
    }
    /// <summary>
    /// 写音频数据
    /// </summary>
    /// <param name="fileSteam"></param>
    /// <param name="clip"></param>
    void ConvertAndWrite(FileStream fileSteam, AudioClip clip)
    {
        // Debug.Log(clip.length+"\n");
        // Debug.Log(clip.samples);
        var samples = new float[clip.samples*clip.channels];
        clip.GetData(samples, 0);
        Int16[] intData = new Int16[samples.Length];
        Byte[] bytesData = new Byte[samples.Length * 2];
        int rescaleFactor = 32767;
        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short) (samples[i] * rescaleFactor);
            Byte[] byteArray = new byte[2];
            byteArray = BitConverter.GetBytes(intData[i]);
            byteArray.CopyTo(bytesData, i * 2);

        }
        fileSteam.Write(bytesData, 0, bytesData.Length);

    }
    /// <summary>
    /// 重写真正的文件头
    /// </summary>
    /// <param name="fileStream"></param>
    /// <param name="clip"></param>
    void WriteHeader(FileStream fileStream, AudioClip clip)
    {
        var hz = clip.frequency;
        var channels = clip.channels;
        var samples = clip.samples;
        fileStream.Seek(0, SeekOrigin.Begin);

        Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
        fileStream.Write(riff, 0, 4);

        Byte[] chunkSize = BitConverter.GetBytes(fileStream.Length - 8);
        fileStream.Write(chunkSize, 0, 4);

        Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
        fileStream.Write(wave, 0, 4);

        Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
        fileStream.Write(fmt, 0, 4);

        Byte[] subChunk1 = BitConverter.GetBytes(16);
        fileStream.Write(subChunk1, 0, 4);

        Byte[] audioFormat = BitConverter.GetBytes(1);
        fileStream.Write(audioFormat, 0, 2);


        Byte[] numChannels = BitConverter.GetBytes(channels);
        fileStream.Write(numChannels, 0, 2);
        Byte[] sampleRate = BitConverter.GetBytes(hz);
        fileStream.Write(sampleRate, 0, 4);
        Byte[] byRate = BitConverter.GetBytes(hz * channels * 2);
        fileStream.Write(byRate, 0, 4);

        UInt16 blockAlign = (ushort)(channels * 2);
        fileStream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

        UInt16 bps = 16;
        Byte[] bitsPerSample = BitConverter.GetBytes(bps);
        fileStream.Write(bitsPerSample, 0, 2);

        Byte[] dataString = System.Text.Encoding.UTF8.GetBytes("data");
        fileStream.Write(dataString, 0, 4);

        Byte[] subChunk2 = BitConverter.GetBytes(samples * channels * 2);//位深为2x8位
        fileStream.Write(subChunk2, 0, 4);
    }

    #endregion

    /// <summary>
    /// 播放音频
    /// </summary>
    public void LoadAudio(string _path){
            if(_path.EndsWith(".wav")){
                ie = GetClip(_path,AudioType.WAV);
            }
            if(_path.EndsWith(".ogg")){
                ie = GetClip(_path,AudioType.OGGVORBIS);
            }
            if(_path.EndsWith(".mp3")){
                ie = GetClipMP3(_path);
            }
            StartCoroutine(ie);
    }

    /// <summary>
    /// 音频数据请求协程
    /// </summary>
    /// <param name="_path">音频路径</param>
    /// <param name="_type">音频类型</param>
    /// <returns>返回迭代器</returns>
    IEnumerator GetClip(string _path,AudioType _type){
        string m_path="";
        m_path = "jar:file://"+_path;
        UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(m_path,_type);

        yield return request.SendWebRequest();
        if(File.Exists(_path)){
            Debug.Log("Responed && EXIST");
            audioSource.clip = DownloadHandlerAudioClip.GetContent(request);
            if(audioSource.clip != null) audioSource.Play();
        }else{
            Debug.Log("File not EXIST");
            Debug.Log(_path);
        }
        originalClip = audioSource.clip;
        setTime(0.0f,audioSource.clip.length);
    }

    /// <summary>
    /// 使用WWW请求MP3格式音频
    /// </summary>
    /// <param name="_path">音频路径</param>
    /// <returns></returns>
    IEnumerator GetClipMP3(string _path){
        string m_path = "jar:file://"+_path;

        WWW www = new WWW(m_path);

        yield return www;

        audioSource.clip = www.GetAudioClip();

        if(audioSource.clip != null) audioSource.Play();

        originalClip = audioSource.clip;
        setTime(0.0f,audioSource.clip.length);
    }
#region Debug
    /// <summary>
    /// 获取目录子文件和子文件夹名称（depth==1）
    /// <para>Debug用</para>
    /// </summary>
    /// <param name="_path">目录路径</param>
    private void D_GetFiles(string _path){
        if(Directory.Exists(_path)){
            Debug.Log("---------FILES--------");
            DirectoryInfo dir = new DirectoryInfo(_path);
            Debug.Log("path: "+_path);
            FileInfo[] files = dir.GetFiles("*",SearchOption.TopDirectoryOnly);
            DirectoryInfo[] childDirs = dir.GetDirectories("*",SearchOption.TopDirectoryOnly); 
            if(files.Length == 0) Debug.Log("NO FILES");

            foreach(var file in files){
                
                Debug.Log("NAME: "+file.Name);
            }

            foreach(var childDir in childDirs){
                if(childDir.Name[0]=='.') continue;
                Debug.Log("DIR_NAME: "+childDir.Name);
            }

        }else{
            Debug.Log("PATH NOT EXIST");
        }
    }
    private void D_test(){
        Debug.Log("test");
    }
#endregion


}
