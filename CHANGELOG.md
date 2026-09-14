## [1.3.0] - 2026-09-14

### Bug Fixes

* 关闭连接时取消输出管道的挂起读取 (#11)
* 修复 SocketSender 池化复用导致服务器 callBack null 崩溃 (#13)
* 修复 ConnectState.CreateConnection 中 stream 参数前的多余空格 (#14)
* 移除 PipelineFilter 默认构造函数限制以支持 DI 构造 (#18)
* 修正测试项目目标框架不匹配

### Documentation

* 按 GameFrameX 规范重建英文 README
* 新增简中、繁中、日、韩四语言 README

### Features

* 公开 IWebSocketCommandMiddleware 接口 (#19)
* 接入 KCP 与 ReliableSession 协议模型

### build

* 升级构建目标至 .NET 10 并同步 global.json 与发布 CI (#20)
* 忽略 gfx-doc 软链文档与 .codegraph 本地索引
* 补充发布流水线
* 移除 global.json SDK 版本钉死
* 升级工作流 action 版本
* 升级 NuGet 包版本并引入集中版本管理

### ci

* 重构同步工作流为可重用外部工作流
## [0.0.1-beta] - 2024-07-04

### DOTNET_CLI_TELEMETRY_OPTOUT

* 1

### Gzip

* fix warning and unpass test cases.

### WIP

* WebSocket Extensions
* WebSocket Extensions
* WebSocket Per-Message Compression Extension
* WebSocket Extension for Per Message Compression
* WebSocket Per-Message Compression Extension

### WPI

* WebSocket Extensions
## [2.0preview1] - 2019-05-16

### GLOBAL

* updated README.TXT

### ServerManager

* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* moved server manager to a new place
* reorganized folder structure
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* re-organized ServerManager
* updated code for SuperSocket 1.5's changes
* added missed logging setup code
* changed to go over secure connection
* updated references
* improved the code
* commit some changes

### ServerManager/mainline

* updated ServerManager for SuperSocket's changes

### Tool

* moved some files from mainline to tools

### Tools

* added PerformanceTestAgent
* added PerformanceTestAgent
* added PerformanceTestAgent
* added PerformanceTestAgent
* added PerformanceTestAgent
* added PerformanceTestAgent
* added PerformanceTestAgent
* added PerformanceTestAgent
* added PerformanceTestAgent
* added PerformanceTestAgent
* added PerformanceTestAgent
* added PerformanceTestAgent
* added PerformanceTestAgent
* added PerformanceTestAgent
* added PerformanceTestAgent
* added PerformanceTestAgent
* added PerformanceTestAgent
* added PerformanceTestConsole project
* added PerformanceTestConsole project
* added PerformanceTestConsole project
* added PerformanceTestConsole project
* added PerformanceTestConsole project
* improved PerformanceTestConsole
* tried to improve PerformanceTestConsole
* tried to improve PerformanceTestConsole
* tried to improve PerformanceTestConsole
* cleaned PerformanceTestConsole
* invoked GC.Collect() before the app quit
* improved performance test tool
* improved performance test tool
* improved performance test tool
* improved performance test tool
* improved performance test tool
* improved performance test tool
* improved performance test tool
* improved performance test tool
* improved performance test tool
* improved performance test tool
* improved performance test tool
* improved performance test tool
* improved performance test tool
* improved PerformanceTestAgent
* improved performance test tool
* removed performance test code
* removed performance test code
* removed performance test code
* removed performance test code

### future

* added some performance test cases
* changed to process received data in working thread pool
* added thread pool size configuration support
* updated test case for RootConfig change
* added log information when ThreadPool size has been changed
* improved performance log
* improved the code processing received data in thread pool
* improved the code reseting thread pool size
* use ConcurrentStack instead of self implemented SynchronizedPool
* use ConcurrentDictionary instead of Dictionary
* removed future branch
* improved custom protocol

### mainline

* fixed a potential bug that after socket server stopped, the running state hadn't been updated!
* improved the code about updating server's running state
* Logged session by IndentityKey instead of sessionID
* Tried to implemented UDP socket server
* Tried to implement test cases of Udp socket server
* tried to implement UdpSocketServer
* Updated test cases of udp socket
* Fixed the issue that we needn't start server anymore if it has been started already!
* changed to initialize tcpConnectedEvent in start method
* don't close the session if no data received
* tried to implement UdpSocketServer
* updated test cases of UdpSocketServer
* still close the connection if no data has been read
* improved the code on detecting whether server is stopped
* improved test cases
* added clearing timeout session test case for UdpSocketServer
* added property SessionCount for AppServer class
* changed idle session timeout unit from minute to second
* improved test cases
* removed unnecessary code line
* Removed unnecessary code for ArraySegmentItem and use ArraySegment<T> instead
* changed methods SearchMark and EndsWith to be generic
* improved test code of UdpSocketServer
* improved ArraySegmentList
* improved test cases for ArraySegmentList
* improved ArraySegmentList
* improved SuperSocket by supporting custom protocol
* improved SuperSocket by supporting custom protocol
* improved SuperSocket by supporting custom protocol
* improved test cases
* improved the custom protocol supporting feature
* added a project "NWebSocket" in QuickStart to show how to implement custom protocol
* added a project "NWebSocket" in QuickStart to show how to implement custom protocol
* added a project "NWebSocket" in QuickStart to show how to implement custom protocol
* added a project "NWebSocket" in QuickStart to show how to implement custom protocol
* added a project "NWebSocket" in QuickStart to show how to implement custom protocol
* added a project "NWebSocket" in QuickStart to show how to implement custom protocol
* added a project "NWebSocket" in QuickStart to show how to implement custom protocol
* maked the protocol configuration to be not required
* made the protocol configuration to be not required
* renamed NWebSocket to SuperWebSocket, because some one took the project name "NWebSocket" in CodePlex
* renamed NWebSocket to SuperWebSocket, because some one took the project name "NWebSocket" in CodePlex
* renamed NWebSocket to SuperWebSocket, because some one took the project name "NWebSocket" in CodePlex
* renamed NWebSocket to SuperWebSocket, because some one took the project name "NWebSocket" in CodePlex
* renamed NWebSocket to SuperWebSocket, because some one took the project name "NWebSocket" in CodePlex
* renamed NWebSocket to SuperWebSocket, because some one took the project name "NWebSocket" in CodePlex
* renamed NWebSocket to SuperWebSocket, because some one took the project name "NWebSocket" in CodePlex
* renamed NWebSocket to SuperWebSocket, because some one took the project name "NWebSocket" in CodePlex
* made the protocol passed from config has higher priority
* removed unnecessary configuration node
* removed unnecessary configuration node
* removed the source code of SuperWebSocket, because SuperWebSocket has became a separated project
* removed the source code of SuperWebSocket, because SuperWebSocket has became a separated project
* removed the source code of SuperWebSocket, because SuperWebSocket has became a separated project
* fixed a bug in ArraySegmentList
* refactored almost most of the code to support custom protocol
* refactored almost most of the code to support custom protocol
* refactored almost most of the code to support custom protocol
* refactored almost most of the code to support custom protocol
* refactored almost most of the code to support custom protocol
* refactored almost most of the code to support custom protocol
* refactored almost most of the code to support custom protocol
* refactored almost most of the code to support custom protocol
* refactored almost most of the code to support custom protocol
* refactored almost most of the code to support custom protocol
* refactored almost most of the code to support custom protocol
* refactored almost most of the code to support custom protocol
* refactored almost most of the code to support custom protocol
* refactored almost most of the code to support custom protocol
* refactored almost most of the code to support custom protocol
* refactored almost most of the code to support custom protocol
* refactored almost most of the code to support custom protocol
* refactored almost most of the code to support custom protocol
* refactored almost most of the code to support custom protocol
* refactored almost most of the code to support custom protocol
* refactored almost most of the code to support custom protocol
* refactored almost most of the code to support custom protocol
* refactored almost most of the code to support custom protocol
* refactored almost most of the code to support custom protocol
* refactored almost most of the code to support custom protocol
* refactored almost most of the code to support custom protocol
* refactored almost most of the code to support custom protocol
* added appendNewLineForResponse option for AppSession
* simplified AppServer and AppSession inheritance
* removed unnecessary file
* removed unnecessary files
* improved SocketContext initializing interface
* improved the code to pass socketContext into command reader
* support new command handler strategy
* made command loading can be extended
* changed SetupCommand interface
* fixed the bug of reusing buffer in AsyncCommandReader
* fixed the bug of reusing buffer in AsyncCommandReader
* fixed the bug of reusing buffer in AsyncCommandReader
* improved logging format
* allowed sending byte array from AppSession directly
* added method GetServerByName and removed the config parameter of Start method for SocketServerManager
* updated for SocketServerManager change
* added broken command block test cases
* Added closed reason support for session
* added a flush in sync session when send binary data to client
* close socket connection when failed to receive a command
* added an extension method GetImplementedObjectsByInterface<TBaseInterface>
* fixed a get configuration value bug
* made CommandInfo's attributes' names shorter
* tried to implement SSL/TLS encryption in sync mode
* tried to implement SSL/TLS encryption in sync mode
* implemented SSL/TLS encryption in sync mode
* added test cases for SSL/TLS encryption of sync mode
* give out an error if enable SSL/TLS security in other socket mode than Sync
* improved the code loading certificate
* changed assembly version number to 1.3
* added .NET 3.5 solution and projects for SuperSocket
* added .NET 3.5 solution and projects for SuperSocket
* added .NET 3.5 solution and projects for SuperSocket
* simplified certificate configuration variables' names
* update QuickStart samples for CommandInfo class changes
* removed two unnecessary lines in AyncSocketSession
* provided default value for properties of ServerConfig model
* added log4net into reference folder
* added log4net into reference folder
* added log4net into reference folder
* changed to use log4net, support IPv6
* changed to use log4net, support IPv6
* improved unit test case
* simplified log format
* use AppDomain root to locate the logs directory
* improved the code about detecting max command length
* stopped using readBuffer.Skip(x).Take(y).ToArray() to generate a new array to improve performance
* added performance logging
* changed performance logging interval to 5 minutes
* fix bugs in performance logging
* included new file SocketServerManager.Performance.cs into .NET 3.5 's project file
* commented unnecessary test case
* added a method in CommandReaderBase class
* added a custom protocol sample project in QuickStart solution
* added a custom protocol sample project in QuickStart solution
* added a custom protocol sample project in QuickStart solution
* added a custom protocol sample project in QuickStart solution
* added a custom protocol sample project in QuickStart solution
* added a custom protocol sample project in QuickStart solution
* added a custom protocol sample project in QuickStart solution
* corrected CustomProtocol project's folder name
* corrected CustomProtocol project's folder name
* corrected CustomProtocol project's folder name
* corrected CustomProtocol project's folder name
* corrected CustomProtocol project's folder name
* corrected CustomProtocol project's folder name
* corrected CustomProtocol project's folder name
* corrected CustomProtocol project's folder name
* fixed a bug in BroadcastService sample project
* removed unnecessary references in Common project
* merged recently changes of future branch to mainline
* fixed a bug about taking session snapshot
* fixed a UdpSocket test case bug
* improved session closed API
* added projects files and solution file for Mono support
* don't arrange too much buffer units
* added QuickStart project in Mono solution
* handle unhandled exception in thread of thread pool for Mono's bug
* committed some code about ConnectionFilter and CommandFilter
* committed some code about ConnectionFilter and CommandFilter
* committed some code about ConnectionFilter and CommandFilter
* implemented connection filter
* implemented connection filter
* improved connection filter
* submitted code about connection filter
* tried to implement command filter
* updated project files of .NET 3.5
* fixed a .net 3.5 compatibility issue
* added SafeCloseClientSocket extension method
* shutdown the socket if the socket is disallowed
* run connection session in thread from thread pool
* fixed a configuration bug
* upgraded version to 1.4
* simplified and improved asynchronous extensions
* return null session if sessionKey is null or empty when getting session by key
* changed serviceName to name for service configuration node
* added solution file for VS2008
* added new sample project "SocksServer" in QuickStart
* added new sample project "SocksServer" in QuickStart
* added new sample project "SocksServer" in QuickStart
* added new sample project "SocksServer" in QuickStart
* added new sample project "SocksServer" in QuickStart
* tried to implement SocksServer
* tried to implement SocksServer
* tried to implement SocksServer
* tried to implement SocksServer
* moved BufferManager to SuperSocket.Common project
* tried to implement SocksServer
* tried to implement SocksServer
* tried to implement SocksServer
* removed SocksServer from QuickStart
* removed SocksServer from QuickStart
* removed SocksServer from QuickStart
* shorten session log message
* checked in build task file
* updated build task file
* updated build task file
* updated build task file
* updated build task file
* fixed build task file bug
* updated build task file
* improved the code about custom protocol setting
* improved the code about performance logging
* improved build task
* improved build task
* improved build task file
* return AsyncTask when you invoke Async.Run
* tried to change TCP keep alive option values
* tried to added WindowsAzure host support
* tried to added WindowsAzure host support
* tried to added WindowsAzure host support
* tried to added WindowsAzure host support
* tried to added WindowsAzure host support
* tried to added WindowsAzure host support
* tried to added WindowsAzure host support
* tried to added WindowsAzure host support
* tried to added WindowsAzure host support
* formated code
* removed built-in connection filters
* removed built-in connection filters
* made KeepAliveTime and KeepAliveInterval configurable
* improved build script
* changed clearIdleSession's default configuration value to false
* simplified session log format
* added serverConfigResolver support
* added copy properties util method and test case
* tried to support Windows Azure Platform
* updated for Windows Azure project
* included more demo projects in Windows Azure package
* added the OnPerformanceDataCollected virtual base method
* improved performance data collected API
* fixed the issue that SendResponse(byte[] data) wasn't implemented in UDP Socket
* simplified next command implementation: If next command reader wasn't set, still use current command reader in next round received data processing
* updated Mono projects files for recently changes
* fixed a logging directory issue if there are many server instances
* fixed a bug that the server still report started success if it failed to initialize
* removed unnecessary configuration file
* fixed a NotImplemented issue in MONO
* removed unnecessary configuration file
* improved the SocketServerManager to prevent to initialize server many times
* improved build script
* improved solution structure
* improved solution structure
* improved solution structure
* improved solution structure
* improved solution structure
* improved solution structure
* improved solution structure
* tried to improve CutomProtocol to support multiple commands in one receiving
* tried to improve CutomProtocol to support multiple commands in one receiving
* improved custom protocol
* extracted AppServerBase
* implemented Policy server for flash and silverlight
* improved build process
* improved build process
* improved build process
* made BufferManager buffer size be only determined by receiveBufferSize
* added configuration sample in QuickStart
* added configuration sample in QuickStart
* added configuration sample in QuickStart
* added configuration sample in QuickStart
* added configuration sample in QuickStart
* corrected flash/silverlight policy server port number
* improved build process
* improved policy server
* fixed sample's issue
* fixed some Linux compatible issue
* fixed some linux compatible issues
* fixed some linux compatible issues
* fixed some linux compatible issues
* removed unused variable from AppServerBase class
* fixed some Linux compatible issues
* fixed some Linux compatible issues
* renamed lot4net config file for unix
* added prefix "SuperSocket." for all SuperSocket projects' names to avoid collisions with user's projects
* updated projects file path for projects' names changing in build file
* updated GlobalAssembly file for version
* changed CommandHandler event to be protected from public
* fixed projects reference issues of .NET 3.5 projects
* added comments for some classes and interfaces
* exposed client socket in ISocketSession
* set NoDelay and DontLinger for socket connection by default
* added another CustomProtocol sample project GPSSocketServer
* added another CustomProtocol sample project GPSSocketServer
* added another CustomProtocol sample project GPSSocketServer
* added another CustomProtocol sample project GPSSocketServer
* added another CustomProtocol sample project GPSSocketServer
* added another CustomProtocol sample project GPSSocketServer
* added another CustomProtocol sample project GPSSocketServer
* improved build process
* improved build process
* improved build process
* fixed build script issue
* fixed build script issue
* updated VS2008's solution file for recent changes of SuperSocket
* improved IAppSever API
* added OnStartup() protected virtual method in AppServer
* fixed a bug cannot get exact exception when fail to get type from a assembly
* created default AppServer and AppSession
* removed SocketContext from SuperSocket
* added DictionaryExtension class in SuperSocket.Common project
* removed async execution feature of command filter
* updated CustomProtocol to support receiving multiple packages in one time
* exposed Logger in IAppSession
* filled comments
* improved command filter execution
* added a basic StringCommandBase class
* added a new QuickStart project CommandFilter
* added a new QuickStart project CommandFilter
* added a new QuickStart project CommandFilter
* added a new QuickStart project CommandFilter
* added a new QuickStart project CommandFilter
* fixed target platform issue of SuperSocket.SocketService project
* fixed a bug in ConnectionFilter
* prevented an exception when close a connection
* added a new sample project ConnectionFilter in QuickStart
* added a new sample project ConnectionFilter in QuickStart
* added a new sample project ConnectionFilter in QuickStart
* added a new sample project ConnectionFilter in QuickStart
* added a new sample project ConnectionFilter in QuickStart
* updated ConnectionFilter quickstart project file
* added a new QuickStart project "MultipleAppServer"
* added a new QuickStart project "MultipleAppServer"
* added a new QuickStart project "MultipleAppServer"
* added a new QuickStart project "MultipleAppServer"
* added a new QuickStart project "MultipleAppServer"
* upgraded Windows Azure project files for SuperSocket changes
* add server instance's name empty detection
* allowed CommandLineProtocol's encoding customizable
* changed default charset of AppSession from default to UTF8
* fixed bugs in receiving data in length or by mark
* removed method GetLeftBuffer() from ICommandReader interface
* improved test case
* changed the location of file log4net.unix.config
* changed the location of file log4net.unix.config
* added file log4net.unix.config into project
* upgraded assembly version number to 1.5
* checked in DLR libraries
* checked in DLR libraries
* checked in DLR libraries
* checked in DLR libraries
* checked in DLR libraries
* added libraries of DLR and IronPython
* added libraries of DLR and IronPython
* added libraries of DLR and IronPython
* added libraries of DLR and IronPython
* added libraries of DLR and IronPython
* added libraries of DLR and IronPython
* added libraries of DLR and IronPython
* added libraries of DLR and IronPython
* added libraries of DLR and IronPython
* fixed a bug that if send a large data to client by SendResponse method in Async mode, may not all data will be sent
* tried to implement dynamic command loader
* tried to implement dynamic command loader
* tried to implement dynamic command loader
* tried to implement dynamic command loader
* tried to implement dynamic command loader
* formatted code
* added QuickStart project IronPythonServer to demo new dynamic command support
* added QuickStart project IronPythonServer to demo new dynamic command support
* added QuickStart project IronPythonServer to demo new dynamic command support
* added QuickStart project IronPythonServer to demo new dynamic command support
* added QuickStart project IronPythonServer to demo new dynamic command support
* added QuickStart project IronPythonServer to demo new dynamic command support
* added QuickStart project IronPythonServer to demo new dynamic command support
* tried to implement dynamic command loader
* improved QuickStart project IronPythonServer
* improved IronPythonServer quickstart project
* let dynamic commands can be updated automatically
* added references of IronJS
* added references of IronJS
* added references of IronJS
* added references of IronJS
* added references of IronJS
* added references of IronJS
* added references of IronJS
* improved dynamic command loader
* improved IronSocketServer quickstart project
* improved IronSocketServer quickstart project
* improved IronSocketServer quickstart project
* improved IronSocketServer quickstart project
* improved IronSocketServer quickstart project
* improved IronSocketServer quickstart project
* improved IronSocketServer quickstart project
* improved IronSocketServer quickstart project
* improved IronSocketServer quickstart project
* improved IronSocketServer quickstart project
* improved the UDP server to support application level session key instead of using IP+PORT
* improved the UDP server's code to update client endpoint of session every time because client's endpoint may change
* moved some classes about async socket to folder AsyncSocket, and fixed some code convention issue of avirami's code
* fixed a reset session identify key bug
* added a test case for new UDP enhancement feature
* added a test case for new UDP enhancement feature
* added a test case for new UDP enhancement feature
* updated .net 3.5 project files for recently changes
* removed the attribute IdentifyKey of session class, you can use SessionID simply
* updated MonoDeveloper project files for recently changes
* added new project SuperSocket.ClientEngine
* added new project SuperSocket.ClientEngine
* added new project SuperSocket.ClientEngine
* worked on ClientEngine
* improved SocketService project to support SocketService can be started by itself without bat file, it is convenient for debugging
* tried to implement client engine
* removed unused project reference for client engine project
* tried to implement ClientEngine
* reference GlobalAssembly.cs in ClientEngine
* removed unused files in ClientEngine
* implement ClientEngine
* implement ClientEngine
* improved ClientEngine
* improved ClientEngine
* tried to support TLS/SSL over Async SslStream
* tried to support TLS/SSL over Async SslStream
* tried to support TLS/SSL over Async SslStream
* tried to support TLS/SSL over Async SslStream
* tried to support TLS/SSL over Async SslStream
* fixed a bug in UDP test case
* removed test case for ReceiveInLength()
* don;t share any code between client engine and SuperSocket common
* unified log4net.config location
* unified log4net.config location
* fixed compiling error in QuickStart
* improved client engine
* implemented a new feature allow loading certificate from certification store
* fixed mono compatible issue for ClientEngine
* removed solution file and project files for Mono, because Mono and .NET can share some files
* removed solution file and project files for Mono, because Mono and .NET can share some files
* improved client engine
* only can use AddressFamily.InterNetwork for client engine
* changed back to use Socket.ConnectAsync to start a connect in ClientEngine
* changed back to use Socket.ConnectAsync to start a connect in ClientEngine
* added separated ClientEngine project for Mono
* promoted recent changes of v1.4 to mainline
* improved ClientEngine
* improved client engine
* improved logging architecture
* improved logging architecture
* improved logging architecture
* improved logging architecture
* improved logging architecture
* improved logging architecture
* improved logging architecture
* improved logging architecture
* improved logging architecture
* removed Sync socket mode support
* renamed CommandInfo to RequestInfo
* fixed bugs in client engine build script
* changed CommandReader to RequestFilter
* changed CommandReader to RequestFilter
* changed CommandReader to RequestFilter
* changed CommandReader to RequestFilter
* changed CommandReader to RequestFilter
* changed CommandReader to RequestFilter
* changed CommandReader to RequestFilter
* changed CommandReader to RequestFilter
* changed CommandReader to RequestFilter
* changed CommandReader to RequestFilter
* changed CommandReader to RequestFilter
* changed CommandReader to RequestFilter
* added WindowsPhone version for ClientEngine
* improved ClientEngine
* made session.SendResponse(data, offset, length) as default binary data sending method
* improved client engine
* improved client engine to support TLS/SSL
* improved client engine to support TLS/SSL
* improved client engine
* fixed the stateful search bug in ClientEngine
* improved async help class
* improved configuration base class
* removed file not in use
* removed file not in used
* removed file not in use from project file
* enabled keep alive for client
* enabled keep alive for client
* improved socket listener
* improved socket listener
* fixed a socket connecting bug in ClientEngine
* added receivedBufferSize support for client
* improved client's receive buffer size
* improved client's receive buffer size
* improved assembly signing procedure
* added log initializing code in SocketService project
* changed maxCommandLength to maxRequestLength
* added ResetSessionSecurity method for AppServer
* checked in initial projects framework of "SuperSocket Server Manager"
* checked in initial projects framework of "SuperSocket Server Manager"
* checked in initial projects framework of "SuperSocket Server Manager"
* checked in initial projects framework of "SuperSocket Server Manager"
* checked in initial projects framework of "SuperSocket Server Manager"
* checked in initial projects framework of "SuperSocket Server Manager"
* checked in initial projects framework of "SuperSocket Server Manager"
* checked in initial projects framework of "SuperSocket Server Manager"
* checked in initial projects framework of "SuperSocket Server Manager"
* checked in initial projects framework of "SuperSocket Server Manager"
* checked in initial projects framework of "SuperSocket Server Manager"
* checked in initial projects framework of "SuperSocket Server Manager"
* checked in initial projects framework of "SuperSocket Server Manager"
* checked in initial projects framework of "SuperSocket Server Manager"
* checked in initial projects framework of "SuperSocket Server Manager"
* checked in initial projects framework of "SuperSocket Server Manager"
* checked in initial projects framework of "SuperSocket Server Manager"
* checked in initial projects framework of "SuperSocket Server Manager"
* checked in initial projects framework of "SuperSocket Server Manager"
* checked in initial projects framework of "SuperSocket Server Manager"
* checked in initial projects framework of "SuperSocket Server Manager"
* checked in initial projects framework of "SuperSocket Server Manager"
* checked in initial projects framework of "SuperSocket Server Manager"
* checked in initial projects framework of "SuperSocket Server Manager"
* checked in initial projects framework of "SuperSocket Server Manager"
* checked in initial projects framework of "SuperSocket Server Manager"
* checked in initial projects framework of "SuperSocket Server Manager"
* checked in initial projects framework of "SuperSocket Server Manager"
* checked in initial projects framework of "SuperSocket Server Manager"
* checked in initial projects framework of "SuperSocket Server Manager"
* checked in initial projects framework of "SuperSocket Server Manager"
* checked in initial projects framework of "SuperSocket Server Manager"
* checked in initial projects framework of "SuperSocket Server Manager"
* checked in initial projects framework of "SuperSocket Server Manager"
* checked in initial projects framework of "SuperSocket Server Manager"
* checked in initial projects framework of "SuperSocket Server Manager"
* checked in initial projects framework of "SuperSocket Server Manager"
* checked in initial projects framework of "SuperSocket Server Manager"
* checked in initial projects framework of "SuperSocket Server Manager"
* tried to implement Server Manager
* tried to implement Server Manager
* tried to implement Server Manager
* improved the ClientEngine to support set client access policy protocol for Silverlight
* tried to implement Server Manager
* tried to implement Server Manager
* tried to implement Server Manager
* tried to implement Server Manager
* tried to implement Server Manager
* tried to implement Server Manager
* tried to implement Server Manager
* tried to implement Server Manager
* tried to implement Server Manager
* tried to implement Server Manager
* tried to implement Server Manager
* tried to implement Server Manager
* tried to implement Server Manager
* tried to implement Server Manager
* tried to implement Server Manager
* renamed TotalHandledCommands to TotalHandledRequests
* added server instance startup order configuration support!
* tried to implement server manager
* tried to implement server manager
* updated SuperWebSocket in server manager
* updated WebSocket4Net in server manager
* tried to implement Server Manager
* tried to implement server manager
* fixed server startup issue
* tried to implement Server Manager
* tried to implement Server Manager
* tried to implement Server Manager
* added a configuration for performance data collecting interval
* tried to implement Server Manager
* tried to implement Server Manager
* improved performance of ClientEngine
* tried to implement Server manager
* fixed some bugs about logging
* tried to implement server manager
* tried to implement server manager
* tried to implement server manager
* ignore some kinds of exceptions
* tried to implement server manager
* tried to implement server manager
* tried to implement server manager
* tried to implement server manager
* tried to implement server manager
* tried to implement server manager
* tried to implement server manager
* tried to implement server manager
* tried to implement server manager
* tried to implement server manager
* tried to implement server manager
* tried to implement server manager
* tried to implement server manager
* improved the performance data collecting
* changed to PerformanceCounter to track performance
* tried to implement server manager
* improved performance tracking
* added xml documentation for Common project
* added configuration DisablePerformanceDataCollector
* added XML documentation comments
* generated XML for Common in Release configuration
* fixed return type issue of IRequestInfoParser
* fixed a bug in accept new client
* improved performance collecting
* changed to fire NewClientAccepted event asynchronously
* ignored some socket exceptions
* added ServerManager for .NET 3.5
* added XML documentation comment
* ignore more socket error
* ignore more socket exception
* added SampleServer for Server Manager
* added SampleServer for Server Manager
* added SampleServer for Server Manager
* added SampleServer for Server Manager
* added SampleServer for Server Manager
* added SampleServer for Server Manager
* added SampleServer for Server Manager
* added SampleServer for Server Manager
* renamed CommandHandler to RequestHandler
* improved async stream initializing
* canceled the change of 76193
* removed the code of ClientEngine
* removed the code of ClientEngine
* improved SuperSocket API
* improved certificate loading API
* don't make Certificate property virtual
* fixed some Linux compatible issues

### mainline/ClientEngine

* added AllowUnstrustedCertificate option for SslStreamTcpSession
* renamed async to raiseEvent because one user reported async caused his building error

### mainline/v1.4

* added a method TrimEnd(int trimSize) for ArraySegmentList
* support disable session snapshot
* improved exception logging in some places
* supported custom child configuration
* improved AssemblyUtil class
* supported custom child configuration
* fixed a bug about getting child configuration
* fixed a bug of "StartSession()" method won't be fired for UdpSession
* fixed an exception when stop UdpSocketServer in Mono
* changed test cases running port
* adjusted sending encoding of test cases for Mono on Linux
* changed listening port of test cases to from 1025 to 1026
* changed the listening port of EchoService in QuickStart to 1026
* added configurable depended services features for SocketService
* improved log4net configuration
* fixed a bug that using AsyncStreamSocketServer instead of AsyncSocketServer by mistake
* fixed an issue about ArraySegmentList encode mask
* fixed a stateful search bug
* fixed a stateful search bug
* fixed a stateful search bug
* improved the code about command filter execution
* minimized projects references
* updated SuperWebSocket for Server Manager
* updated SuperWebSocket for Server Manager
* improved project files and build script
* updated SuperWebSocket and WebSocket4Net for Server Manager
* Updated WebSocket4Net for Server Manager
* removed ServerManager in old place
* removed ServerManager in old place
* removed ServerManager from build script

### v1.0

* fixed a potential bug that after socket server stopped, the running state hadn't been updated
* fixed clear timeout session synchronization issue
* fixed a bug in ArraySegmentList
* fixed a bug on getting configuration value
* updated assembly version number for 1.0

### v1.4

* fixed a bug that if send a large data to client by SendResponse method in Async mode, may not all data will be sent
* support SendResponse(data, offset, length) for socket session
* enhanced UdpSocketServer to support application level session key
* enhanced UdpSocketServer to support application level session key
* enhanced UdpSocketServer to support application level session key
* updated assembly version
* updated Mono project files
* support TLS/SSL over Async SslStream
* removed two method "ReceiveData(...)" from SocketSession
* added NextCommandReader property to AppSession
* moved log4net config files
* fixed compiling error
* removed solution file and project files for mono, you can use .NET 4 assemblies for Mono
* removed solution file and project files for mono, you can use .NET 4 assemblies for Mono
* improved performance ArraySegmentList and CommandLineProtocol
* added windows service description support in configuration
* improved ArraySegmentList
* improved ArraySegmentList
* improved performance of searching mark
* improved performance slightly
* changed the listening ports of samples applications in QuickStart to 2012
* removed old build task file
* updated assembly version
* updated assembly information
* improved SearchMarkState class
* fixed output dir bug in a .net 3.5 project file
* added socket listening backlog configurable support
* fixed a bug about build solution with a strongly name
* updated revision in global assembly info class
* added NuGet specification file
* updated assembly info
* update nuget spec file
* removed unused file
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* implement server manager for v1.4
* updated assembly version
* updated SuperWebSocket for Server Manager
* added configuration DisablePerformanceDataCollector
* improved performance collecting
* added Server manager of .NET 3.5
* added SampleServer for Server Manager
* added SampleServer for Server Manager
* added SampleServer for Server Manager
* added SampleServer for Server Manager
* added SampleServer for Server Manager
* added SampleServer for Server Manager
* added SampleServer for Server Manager
* adjusted performance log message
* updated assembly version
* fixed a bug about initializing servers by programming
* added appServer started time into performance log
* fixed bug about custom child configuration
* fixed bug about custom child configuration
* fixed bug about custom child configuration

### v1.4/mainline

* fixed a bug about getting policy file path in policy server
* fixed a Mono compatibility bug about custom configuration

### v1.4/v1.5

* removed MiaSocks from code
* removed MiaSocks from code
* fixed performance log issue in Linux

### v1.5

* replaced SocketServerManager with Bootstrap
* replaced SocketServerManager with Bootstrap
* removed useless file
* removed useless file from project
* renamed extension method Perf of LoggingExtensions to LogPerf
* removed useless file
* removed useless file from project
* improved async sending
* improved server engine console startup output information
* changed method name "SafeCloseClientSocket()" to "SafeClose()"
* improved the code about connection initialization
* improved console startup output information
* removed the configuration attribute "ReadTimeOut"
* added ApplicationError into CloseReason
* fixed a spelling issue in comment
* removed unused configuration attributes
* added SyncSend configuration
* fixed a Mono compiling compatibility issue
* fixed a warning
* better service detection in SocketService
* fixed a configuration issue
* improved fixed size request reader
* tried to implement UDP mode
* tried to implement UDP server
* implement UDP server
* updated latest active time when sending data
* improved QuickStart
* improved QuickStart
* removed useless code
* improved QuickStart
* improved connection filter
* improved the samples in QuickStart
* changed some classes to inherit from MarshalByRefObject
* made the logger of each appserver can be setup differently
* upgraded version number
* fixed two issues about configuration
* updated assembly version
* removed MarshalByRefObject inheritance
* fixed UDP socket listening issue
* added fixed header request filter base class
* added fixed header request filter base class
* added fixed header request filter base class
* improved logging
* improved logging
* improved logging
* improved logging
* improved logging
* tried to implement AppDomain level isolation
* tried to implement AppDomain level isolation
* tried to implement AppDomain level isolation
* tried to implement AppDomain level isolation
* improved test cases
* adjusted references of logging for Enterprise Library
* fixed some provider factories issue
* improved exception catch in CommandLoader
* fixed SuperSocket startup issue
* changed listener configuration format
* corrected configuration issues
* improved command loader API
* removed IronJS from Reference
* removed useless comment
* updated configuration file for IronPython sample project
* improved DLR command implementation
* removed ClientEngine folder
* improved the code about command filter loading
* improved the code about registering new session into session container and getting session by ID
* removed logging extensions from source code
* updated assembly version
* fixed a stackoverflow exception in WorkItemFactoryInfoLoader
* removed unused file
* added test cases for DLR commands and connection filter
* added test cases for DLR commands and connection filter
* added test cases for DLR commands and connection filter
* fixed AppDomain isolation issues
* fixed AppDomain isolation issues
* fixed AppDomain isolation issues
* fixed some AppDomain isolation issues
* moved basic logging classes from Common to SocketBase
* moved basic logging classes from Common to SocketBase
* moved basic logging classes from Common to SocketBase
* added the function creating a bootstrap from specific configuration file
* checked in missed files
* checked in missed files
* checked in missed files
* fixed a bug about custom child configuration support
* improved test cases
* renamed IsolationMode to Isolation
* improved concurrent sending function
* improved concurrent sending code
* improved concurrent sending
* improved concurrent sending
* added missed file
* improved sending queue test case
* renamed session.SendResponse to session.Send
* added a overload method for BootstrapFactory.CreateBootstrap
* improved the sending code
* improved configuration

### v1.5/v1.4

* fixed a bug in AssemblyUtil.CopyPropertiesTo(xxx)

### v1/5

* removed useless attribute "enableDynamicCommand" from server configuration

### v15

* improved command setup
<!-- generated by git-cliff -->
