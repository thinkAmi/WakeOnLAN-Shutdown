WakeOnLAN-Shutdown
==================

## Getting Started

1. Clone  
 `git clone https://github.com/thinkAmi/WakeOnLAN-Shutdown.git`  
　
2. Build in Visual Studio  
　
3. Edit `TargetMachine.csv`  
 Add `Host_Name`, `MAC_Address`  
　
4. Put in the same directory
 *  `WakeOnLAN-Shutdown.exe`
 * `TargetMachine.csv`
 * `SharpPcap.dll`
 * `PacketDotNet.dll`
 * `CommandLine.dll`  
　
　
5. Wake On LAN  
 `WakeOnLAN-Shutdown.exe -w`  

　

## Usage

### Wake on LAN 
 `WakeOnLAN-Shutdown.exe -w`  
　
### Shutdown
 `WakeOnLAN-Shutdown.exe -s`  
　
### Update `TargetMachine.csv` file (MAC_Address field)   
 `WakeOnLAN-Shutdown.exe -u`  
 (Note: target is power on)  
　

## Tested environment
 * Windows7 x64
 * VisualStudio 2012
 * .NET Framework 4
 * NuGet 2.8
 * SharpPcap 4.2.0
 * CommandLineParser 1.9.71


## License
MIT  
　
### Library License
 * [SharpPcap](http://sourceforge.net/projects/sharppcap) - LGPL
 * [CommandLineParser](http://commandline.codeplex.com/) - MIT
