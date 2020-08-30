# 项目简介

cx-auto-sign 是基于 dotnet core 的超星学习通自动签到工具。
本项目支持以下两种监听新签到任务事件的方式：

1. 轮询各个课程的活动任务页，检查是否有新的签到任务
2. 通过超星学习通的即时通讯协议，如果指定课程有新的消息事件，则检查该课程是否有新的签到任务。(就是监听学习通App的课程聊天群组有没有新的消息)

**方式1** 需要以很高的频率访问超星学习通，频率低了有错过签到的风险。

**方式2** 的原理是老师的签到任务也是一条消息，因此理论上能通过即时通讯协议接收到该消息。本项目最低程度地实现了学习通的即时通讯协议。在接收到指定课程的消息后，就会检查该课程有无新的签到任务，然后进行签到。

# 项目进度

- [x] 支持账号登录和学号登录两种登录方式
- [x] 支持 `init`指令，用以生成配置文件
- [x] 实现基于**方式2**的自动签到工作流程
- [x] 优化命令行的日志显示
- [ ] 支持签到成功后发送邮件通知
- [ ] 实现基于**方式1**的自动签到工作流程


# 使用方法 (for windows)

## 0x00 运行环境

无论是 Windows 还是 Linux，都需要在[.Net Core Runtime 下载页](https://dotnet.microsoft.com/download/dotnet-core/current/runtime)下载并安装 **.Net Core 3.1 Runtime** (或SDK)。

然后在[Release页面](https://github.com/cyanray/cx-auto-sign/releases)下载 cx-auto-sign.zip，并解压到某个目录。

## 0x01 登录并初始化配置文件

在 cx-auto-sign.exe 所在的目录执行以下命令行:

```powershell
# 通过手机号码登录，不需要学校编码
./cx-auto-sign.exe init -u "双引号里面填手机号" -p "双引号里面填密码" 
```

**或：**

```powershell
# 通过学号登录，需要学校编码
./cx-auto-sign.exe init -u "双引号里面填学号" -p "双引号里面填密码" -f "学校编码"
```

以上指令会创建 **AppConfig.json** 文件以及 **Courses** 目录。其中 **Courses** 目录下有一系列 **.json** 文件，每个文件对应一门课程。对于不需要自动签到的课程，请删除对应的文件。

## 0x02 开始自动签到

在 cx-auto-sign.exe 所在的目录执行以下命令行:

```powershell
./cx-auto-sign.exe work
```

即可开始自动签到。

# 使用方法 (for linux)

使用方法和 windows 平台是差不多的，略有不同：

```bash
# 通过手机号码登录，不需要学校编码
dotnet cx-auto-sign.dll init -u "双引号里面填手机号" -p "双引号里面填密码" 

# 通过学号登录，需要学校编码
dotnet cx-auto-sign.dll init -u "双引号里面填学号" -p "双引号里面填密码" -f "学校编码"

# 开始自动签到
dotnet cx-auto-sign.dll work
```


# 声明

一切开发旨在学习，请勿用于非法用途。
