package main

import (
	///"fmt"
	"log"
	"time"

	"github.com/xtaci/kcp-go/v5"
)

func main() {
	// 连接到服务端的 10000 端口
	// 本地测试，用 127.0.0.1
	// KCP基于UDP，所以不需要像TCP那样建立三次握手，但Dial会初始化KCP会话对象
	sess, err := kcp.DialWithOptions("127.0.0.1:10000", nil, 10, 3)
	if err != nil {
		log.Fatal(err)
	}

	log.Println("已连接到服务端！")

	//发送一条消息
	data := []byte("Hello Server! I am User 1")
	_, err = sess.Write(data)
	if err != nil {
		log.Fatal(err)
	}
	log.Println("发送成功:", string(data))

	// 等待服务端的回复 (Echo)
	// 用于接收服务端回复的数据
	buf := make([]byte, 4096)
	n, err := sess.Read(buf)
	if err != nil {
		log.Fatal(err)
	}

	// 打印回复
	log.Println("收到服务端回复:", string(buf[:n]))

	// 保持一小会儿，避免程序直接退出
	time.Sleep(1 * time.Second)
}
