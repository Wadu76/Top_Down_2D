package main

import (

	"log"
	"google.golang.org/protobuf/proto"
	"top-down-2d/pkg/pb"
	
	"github.com/xtaci/kcp-go/v5"

	
)

func StartNetworkServer() {
	//监听端口10000
	//ListenWithOptions函数用于监听指定端口，
	//创建带自定义选项的KCP监听器，最终返回 *kcp.Listener（KCP 监听器实例） 和error
	// 参数1为端口号
	// 参数2为kcp的加密配置，暂时设为空
	// 参数3为纠删码中的数据分片数量（用于数据容错，指定原始数据拆分的分片数）
	// 参数4为纠删码中的校验分片数量（用于数据容错，指定额外生成的校验分片数）
	//listener, err := kcp.ListenWithOptions(":10000", nil, 10, 3)

	listener, err := kcp.ListenWithOptions(":10000", nil, 0, 0)
	if err != nil {
		// 端口被占用就退出
		// log为go的日志包
		// Fatal是输出错误日志的方法，打印错误消息后立即终止程序运行，并返回一个非零的退出状态码
		log.Fatal(err)
	}

	//若没退出说明端口没被占用，监听成功
	log.Println("listening on port :10000")

	for { //无退出条件
		//等待客户连接. AcceptKCP函数用于接受客户端KCP连接，返回 *kcp.UDPConn（KCP 连接实例） 和error
		//相当于阻塞操作，等待客户端连接前会在本行等待
		conn, err := listener.AcceptKCP()
		if err != nil {
			log.Fatal(err)
		}

		//为每个新连接启动一个goroutine处理连接，该循环为无限循环
		// 并发服务器即如此实现
		go handleConnection(conn, eventChannel)
	}
}


func handleConnection(conn *kcp.UDPSession, eventChan chan<- Event) {
	log.Println("new player connected:", conn.RemoteAddr())

	// KCP的参数调整（需要快）
	// NoDelay函数用于设置KCP的快速模式，参数依次为：
	// nodelay: 是否启用快速模式，1表示启用
	// 10: 协议内部工作的间隔时间，单位为ms
	// 2：快速重传（丢包两次就重传）
	// 1；关闭流控，不被网络拥堵而自动减速
	conn.SetNoDelay(1, 10, 2, 1)
	// 设置最大窗口大小(流量控制 发送窗口&接收窗口)
	// 通过控制数据发送速率来平衡传输效率和网络稳定性
	conn.SetWindowSize(128, 128)
	// 立即发送ACK，收到数据包就发送。
	// 提高响应速度，带宽消耗多
	conn.SetACKNoDelay(true)

	defer conn.Close()

	// 一个4kb大的缓冲区用于读数据并发送（echo）
	// 用于接收客户端发送的数据
	buf := make([]byte, 4096)

	//不想看红报错
	//_ = proto.Marshal
	//_ = &pb.PlayerInput{}
	countID := 1

	for {
		// 读取数据
		//n为读取到的字节数
		n, err := conn.Read(buf)
		if err != nil {
			//服务器端打印日志，客户端看不到
			log.Println("read error:", err)
			return
		}
		// 收到的
		//msg := string(buf[:n])
		//拖死服务器 发的的是二进制Protobuf，强行转string打印出来的也是乱码，不仅没用，还会导致服务器卡死。
		//log.Printf("received message: %s", msg)

		//判断是否接收到数据,不能<0
		if n < 1 {
			log.Println("no data received")
			continue
		} else {
			//解析数据
			//数据是对应什么操作的
			msgID := buf[0]
			//对应操作的数据
			//realData := buf[1:] 会读多余脏shuju
			realData := buf[1:n]

			event := Event{
				ConnId : countID,
				MsgID:  msgID,
				RawData:   realData,
			}
			/*
				定义消息ID (口头协议)
				在代码里写死常量

				1 = LoginRequest	客户端 -> 服务端

				2 = LoginResponse	服务端 -> 客户端

				3 = PlayerInput		客户端 -> 服务端

				4 = PlayerState     服务端 -> 客户端

				5 = WorldState		服务端 -> 客户端
			*/
			

		}
	}
}


func SendMessagetoClient(conn *kcp.UDPSession, msgID byte, data proto.Message) {
	//序列化
	body, err := proto.Marshal(data)
	if err != nil {
		log.Println("marshal error:", err)
		return
	}

	//封装
	//ID+Body
	packet := make([]byte, len(body)+1)
	packet[0] = msgID
	copy(packet[1:], body)

	//发送
	_, err = conn.Write(packet)
	if err != nil {
		log.Println("write error:", err)
	}
}