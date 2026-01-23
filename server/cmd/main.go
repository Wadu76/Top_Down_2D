package server

import (
    "fmt"
    kcp "github.com/xtaci/kcp-go/v5"
)

func main() {
    listener, err := kcp.Listen(":10000")
    if err != nil {
        panic(err)
    }
    defer listener.Close()
    fmt.Println("KCP server listening on :10000")
}
