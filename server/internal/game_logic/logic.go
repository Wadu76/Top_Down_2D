package main

import (
	"log"
	"top-down-2d/pkg/pb"

	"google.golang.org/protobuf/proto"
)

// StartLogicProcessor 启动游戏逻辑循环
func StartLogicProcessor(eventChan <-chan Event) {
	log.Println("Logic Processor Started")

	for {
		// 1. 从 Channel 阻塞获取事件
		event := <-eventChan

		// 2. 根据消息ID分发逻辑
		switch event.MsgID {
		case 1: // LoginRequest
			handleLogin(event)
		case 3: // PlayerInput
			handlePlayerInput(event)
		default:
			log.Printf("Unknown MsgID: %d from ConnID: %d", event.MsgID, event.ConnID)
		}
	}
}

// handleLogin 处理登录逻辑
func handleLogin(event Event) {
	req := &pb.LoginRequest{}
	// 在逻辑层进行反序列化
	if err := proto.Unmarshal(event.RawData, req); err != nil {
		log.Println("Unmarshal LoginRequest error:", err)
		return
	}

	log.Printf("Logic: Player [%d] login attempt: Name=%s", event.ConnID, req.Name)

	// 业务逻辑：验证通过
	resp := &pb.LoginResponse{
		Success: true,
	}

	// 实际开发中，Event 里应该包含 Conn 对象，或者通过 ConnID 查找 Conn 对象。
	// 演示代码仅打印日志
	log.Println("Logic: Sending Login Success to player")
}

// handlePlayerInput 处理玩家输入逻辑
func handlePlayerInput(event Event) {
	input := &pb.PlayerInput{}
	if err := proto.Unmarshal(event.RawData, input); err != nil {
		log.Println("Unmarshal PlayerInput error:", err)
		return
	}

	// 业务逻辑：计算新位置
	// 这里可以添加复杂的物理运算、碰撞检测等
	newState := &pb.PlayerState{
		Id:   int32(event.ConnID),
		PosX: input.MoveX,
		PosY: input.MoveY,
	}

	log.Printf("Logic: Player [%d] moved to (%.2f, %.2f)", event.ConnID, newState.PosX, newState.PosY)
}



