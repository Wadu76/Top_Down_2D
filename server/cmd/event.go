package main

//Event is a struct for holding event information
//用于在Network和Logic层之间传消息

type Event struct {
	//连接标识
	ConnID int
	//消息类型	
	MsgID byte
	//消息内容 RealData
	RawData []byte
	
}