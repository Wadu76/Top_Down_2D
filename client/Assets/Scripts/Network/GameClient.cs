using System.Collections;
using UnityEngine;
using Google.Protobuf;
using Game.Protocol;
using System;
using kcp2k;
using UnityEditor.VersionControl;//proto生成的协议
public class GameClient : MonoBehaviour
{
    public static GameClient Instance;
    //
    public SimpleNetworkManager net;

    //定义事件：登录成功
    public event Action<int> OnLoginSuccess;
    //定义事件：玩家状态改变(int:ID, float:X, float:Y)
    public event Action<int, float, float> OnPlayerStateUpdate;

    //一定要最早初始化
    void Awake()
    {
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void ProcessMessage(byte[] packet)
    {
        //
        Debug.Log($"[GameClient] 收到原始包，长度: {packet.Length}, ID: {packet[0]}");
        //起码得有数据才能处理

        if (packet.Length < 1)
        {
            return;
        }

        //头部为消息类型ID，用于识别是哪种类型的ID
        byte msgID = packet[0];

        //去头取尾，获取原本的消息
        byte[] body = new byte[packet.Length - 1];
        Buffer.BlockCopy(packet, 1, body, 0, packet.Length - 1);

        //网络传输不稳定，防止格式不匹配 所以要捕获错误（learned from netcoding from youtube）
        //protobuf没更新解析也会影响？
        try
        {
            switch(msgID)
            {
                //登录响应 LoginResponse：2（消息类型ID：2）
                case 2:
                    LoginResponse loginResp = LoginResponse.Parser.ParseFrom(body);
                    //如果响应正确
                    if(loginResp.Success == true)
                    {
                        Debug.Log($"登录成功，ID为: {loginResp.Id}");
                        //触发登陆成功事件
                        OnLoginSuccess?.Invoke(loginResp.Id);
                    }
                    break;

                //玩家状态 PlayerState ：4
                case 4:
                    PlayerState state = PlayerState.Parser.ParseFrom(body);

                    //debug：解析成功了吗？ID 是多少？
                    Debug.Log($"[Client] 解析移动包: ID={state.Id}, X={state.PosX}, Y={state.PosY}");
                    //触发玩家状态事件,会触发移动
                    OnPlayerStateUpdate?.Invoke(state.Id, state.PosX, state.PosY);
                    break;
            }
        }
        //捕获错误
        catch (Exception e)
        {
            Debug.LogError($"ID:{msgID}解析出错 {e}");
        }
    }

    //下面是发送消息相关函数

    //发送登录请求LoginRequest:1
    public void SendLogin(string name)
    {
        LoginRequest req = new LoginRequest {Name = name };
        SendPacket(1, req);
    }

    //发送移动
    public void SendMove(float x, float y)
    {
        PlayerInput input = new PlayerInput
        {
            MoveX = x,
            MoveY = y,
            Fire = false,
            Time = DateTime.Now.Ticks,
        };
        //发给服务端的ID是3
        SendPacket(3, input);
    }

    //发送消息函数
    public void SendPacket(byte id, IMessage msg)
    {
        //信息部分序列化
        byte[] body = msg.ToByteArray();
        //body是具体信息 还要加一位消息类型ID
        byte[] packet = new byte[body.Length + 1];
        //头部的消息类型ID
        packet[0] = id;
        Buffer.BlockCopy(body, 0, packet, 1, body.Length);

        //写好数据别忘了送出去啊喂
        if (net != null)
        {
            net.SendBytes(packet);
        }
        else
        {
            Debug.LogError("SimpleNetworkManager没连接上");
        }
    }
}
