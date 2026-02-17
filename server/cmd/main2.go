package main

import (
	"fmt"
)

func main2(){

	fmt.Print("Hello World")
	eventChannel := make(chan Event, 1000)
	go StartLogicProcessor(eventChannel)
	go StartNetworkServer(eventChannel)



}