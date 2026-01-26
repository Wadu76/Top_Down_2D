using UnityEngine;
using System.Net;
using System.Net.Sockets;
using kcp2k;

public class SimpleNetworkManager : MonoBehaviour
{
    //配置
    public string address = "127.0.0.1";
    public ushort port = 10000;

    //底层UDP
    //.NET的UDP客户端对象，负责实际的UDP数据包收发
    private UdpClient udpClient;
    //封装服务器的IP+端口，作为UDP通信的目标地址
    private IPEndPoint serverEndPoint;

    //KCP核心对象 处理KCP协议的分片、重传、拥塞控制等
    private Kcp kcp;

    //会话ID (需与服务器一致，暂时写死)
    //KCP的会话ID（Conv = Conversation ID），客户端和服务器必须一致才能识别同一个会话
    const uint CONV = 1001;

    //标记是否已建立连接（逻辑层面，非TCP的“连接”，UDP无连接）
    bool isConnected = false;

    void Start()
    {
        //准备 UDP
        //解析IP字符串为IPAddress对象，封装成服务器端点
        serverEndPoint = new IPEndPoint(IPAddress.Parse(address), port);
        //实例化UDP客户端，默认会自动绑定一个本地随机端口
        udpClient = new UdpClient();
        //让UDP客户端“连接”到服务器端点，设置 UDP 客户端的默认发送目标
        //后续调用udpClient.Send()时，无需再指定目标地址，直接发向这个端点
        udpClient.Connect(serverEndPoint);

        //初始化 KCP
        //参数1: Conv ID。会话 ID，服务器必须识别这个 ID 才能处理数据
        //参数2: 发送回调函数KCP内部处理完数据（分片、加头部等）后，会调用这个函数把数据通过UDP发出去
        kcp = new Kcp(CONV, RawSend);

        //调参 同go-server一致
        //参数1 nodelay：是否启用快速模式（1 = 启用，0 = 禁用)
        //参数2 interval：KCP 内部处理逻辑的间隔时间（单位 ms，10 = 每 10ms 处理一次重传 / 拥塞）
        //参数3 resend: 快速重传阈值（2 = 丢包 2 次就立即重传，默认是 0，即不快速重传）
        //参数4 nocwnd：是否禁用流控（true = 禁用，false = 启用；禁用流控会让传输更快，但可能加重网络拥堵）
        kcp.SetNoDelay(1, 10, 2, true);

        //发送窗口和接收窗口大小均为128（单位数据包）
        kcp.SetWindowSize(128, 128);

        //标记初始化完成，后续 Update 中可以执行收发逻辑
        isConnected = true;
        //调试用
        Debug.Log($"<color=green>UDP Socket 已就绪，连接目标 {address}:{port}</color>");

        //发送第一条打招呼消息
        Send("Hello KCP, I am Raw Client!");
    }

    void Update()
    {
        if (!isConnected) return;

        //接收 (Input)
        //从UDP缓冲区捞数据，喂给KCP，缓冲池有数据就开始
        while (udpClient.Available > 0)
        {
            IPEndPoint remote = null;
            byte[] udpData = udpClient.Receive(ref remote);

            //把UDP收到的原始字节数据喂给KCP
            //Input现在需要传 3 个参数：数组，偏移量，长度
            kcp.Input(udpData, 0, udpData.Length);
        }

        //驱动 (Update)，Unity 启动后的秒数，*1000转成毫秒，强转成uint（kcp要求）
        kcp.Update((uint)(Time.time * 1000));

        //读取 (Receive) 
        //PeekSize > 0 说明有完整的包准备好了
        int size = kcp.PeekSize();
        while (size > 0)
        {
            byte[] buffer = new byte[size];

            //Receive 需要传2个参数：数组，最大读取长度
            if (kcp.Receive(buffer, size) > 0)
            {
                OnMessageReceived(buffer);
            }
            //看看还有没有下一个
            size = kcp.PeekSize();
        }

        //测试输入
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Send("Ping: " + Time.time);
        }
    }

    //回调与功能函数

    //KCP内部算好要发的数据后，调用这个函数真正发出去
    //这里kcp2k底层回调回来的参数也变了，对应修改
    void RawSend(byte[] data, int size)
    {
        if (udpClient != null)
        {
            //直接把data发出去，length就是size
            udpClient.Send(data, size);
        }
    }

    public void Send(string content)
    {
        if (!isConnected) return;

        //字符串不能直接网络传输，必须转成字节数组 UTF8是跨平台的编码方式
        byte[] data = System.Text.Encoding.UTF8.GetBytes(content);

        //Send也要传3个参数：应用层数据数组，偏移量，长度
        //KCP会把这些数据分片（如果超过 MTU）、加入发送队列，等待kcp.Update驱动发送
        kcp.Send(data, 0, data.Length);

        Debug.Log($"[Client] 发送: {content}");
    }

    //处理收到的消息
    void OnMessageReceived(byte[] data)
    {
        string msg = System.Text.Encoding.UTF8.GetString(data);
        Debug.Log($"<color=cyan>[Server]: {msg}</color>");
    }

    //Unity退出时执行
    void OnApplicationQuit()
    {
        isConnected = false;
        //关闭UDP客户端，释放网络资源（端口、缓冲区等）
        if (udpClient != null) udpClient.Close();
    }

    void SendBytes(byte[] data)
    {
        if(kcp != null)
        {
            kcp.Send(data, 0, data.Length);
        }
    }
}