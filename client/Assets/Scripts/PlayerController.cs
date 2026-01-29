using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 5f;
    public int myPlayerId = 1001;//后面改成动态，暂时因为没写新玩家新id
    private float nextSendTime = 0;
    //0.05s发一次
    private float sendRate = 0.05f;

    void Start()
    {
        //监听服务器消息，服务器说的算,状态更新函数订阅上移动函数
        //状态更新触发后移动函数就触发
        GameClient.Instance.OnPlayerStateUpdate += HandleServerMovement;

        //发送个登录请求
        GameClient.Instance.SendLogin("Wadu76");
    }

    private void OnDestroy()
    {
        //取消监听，取消订阅防止内存泄漏
        if (GameClient.Instance != null)
        {
            GameClient.Instance.OnPlayerStateUpdate -= HandleServerMovement;
        }
    }

    // Update is called once per frame
    void Update()
    {
        //获取输入
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        //如果有按键，就发给服务器
        //(为了防抖，可以判断 h!=0 || v!=0)
        if (Time.time >= nextSendTime)
        {
            if (h != 0 || v != 0)
            {
                //预测移动：可以先transform.Translate让自己动，显得不卡
                //这里先做“强同步”，只发不改位置，等服务器回包再改

                //计算想去的新位置 (这里简单发输入方向，或者发计算后的坐标，根据服务端逻辑)
                //Go服务端里我们是直接把MoveX/Y当作坐标回传的，所以我们这里发坐标
                float newX = transform.position.x + h * speed * Time.deltaTime;
                float newY = transform.position.y + v * speed * Time.deltaTime;

                GameClient.Instance.SendMove(newX, newY);

                //更新下次发送时间
                nextSendTime = Time.time + sendRate;
            }
        }
    }

    void HandleServerMovement(int id, float x, float y)
    {
        //只处理我自己的 ID
        //debug：收到通知了，ID对得上吗？
        Debug.Log($"[Player] 收到移动通知: ID={id} (我的ID={myPlayerId}) -> 目标: {x}, {y}");
        if (id == myPlayerId)
        {
            transform.position = new Vector3(x, y, 0);
        }
    }
}
