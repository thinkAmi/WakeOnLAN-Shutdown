using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WakeOnLAN_Shutdown
{
    class Program
    {
        //  ファイル設定など
        private const string TargetMachineFileName = "TargetMachine.csv";
        private static readonly string TargetMachineFileFullPath
            = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                                     TargetMachineFileName);
        private static readonly System.Text.Encoding Encoding = System.Text.Encoding.GetEncoding("shift_jis");


        /// <summary>
        /// エントリポイント
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            var options = new CommandLineOption();
            if (!CommandLine.Parser.Default.ParseArguments(args, options))
            {
                Console.WriteLine("The command line argument is not valid. Verify the switch you are using.");
                return;
            }


            //  コマンドライン引数による処理分岐
            if (options.Update) Execute(UpdateSettingsFile);
            else if (options.WakeOnLan) Execute(WakeOnLan);
            else if (options.Shutdown) Execute(Shutdown);
        }


        /// <summary>
        /// 処理の実行
        /// (スイッチ共通の処理はここに書く)
        /// </summary>
        /// <param name="func"></param>
        static void Execute(Func<List<TargetMachine>, string> func)
        {
            var list = ImportSettingFile();

            var msg = func(list);
            Console.WriteLine(msg);
        }



        /// <summary>
        /// 対象端末ファイルの更新
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        static string UpdateSettingsFile(List<TargetMachine> list)
        {
            var devices = CollectSendableDevices();
            
            //  MACアドレスが取れなかった場合、現在はシャットダウンしている可能性があるため、以前のMACアドレス情報をそのままセット
            var results = new List<TargetMachine>();
            var targets = list.Where(l => !string.IsNullOrWhiteSpace(l.HostName)).ToList();
            targets.ForEach(t => results.Add(new TargetMachine{ HostName = t.HostName,
                                                      MACAddress = ToMACAddress(devices, t.HostName) ?? t.MACAddress
                                                    }));

            ExportSettingFile(results);

            return "Update target machine file.";
        }




        /// <summary>
        /// 対象端末ファイルを元にした、SharpPcapによるWakeOnLANの実行
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        static string WakeOnLan(List<TargetMachine> list)
        {
            var address = System.Net.NetworkInformation.PhysicalAddress.Parse("FF-FF-FF-FF-FF-FF");
            var devices = CollectSendableDevices();

            //  マジックパケットはMACアドレスに対して送るので、MACアドレスの記載があるもののみ対象
            var targets = list.Where(l => !string.IsNullOrWhiteSpace(l.MACAddress));
            foreach (var target in targets)
            {
                //  マジックパケット作り
                var eth = new PacketDotNet.EthernetPacket(address,
                                                          address,
                                                          PacketDotNet.EthernetPacketType.WakeOnLan);
                eth.PayloadPacket = new PacketDotNet.WakeOnLanPacket(System.Net.NetworkInformation.PhysicalAddress.Parse(target.MACAddress));
                byte[] bytes = eth.BytesHighPerformance.Bytes;


                //  自分の端末から目標のMACアドレスへパケット送信
                //  パケット送信が成功したかどうかは分からないため、
                //  すべての送信可能なデバイスより送信する
                devices.ForEach(d =>
                {
                    d.Open();
                    d.SendPacket(bytes);
                    d.Close();
                });
            }

            return "Sent magic packet.";
        }


        /// <summary>
        /// 対象端末ファイルを元にした、リモートシャットダウンの実行
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        static string Shutdown(List<TargetMachine> list)
        {
            list.Where(l => !string.IsNullOrWhiteSpace(l.HostName))
                .ToList()
                .ForEach(l => System.Diagnostics.Process.Start("shutdown", "-s -m " + l.HostName));
            
            return "Sent shutdown command.";
        }




        /// <summary>
        /// 対象端末ファイルの読込み
        /// </summary>
        /// <returns></returns>
        private static List<TargetMachine> ImportSettingFile()
        {
            var list = new List<TargetMachine>();

            using (var parser = new Microsoft.VisualBasic.FileIO.TextFieldParser(TargetMachineFileFullPath, Encoding))
            {
                parser.SetDelimiters(",");
                
                //  一行目はヘッダなので捨てる
                parser.ReadFields();

                while (!parser.EndOfData)
                {
                    list.Add(TargetMachine.Analyze(parser.ReadFields()));
                }
            }

            return list;
        }


        /// <summary>
        /// 対象端末ファイルの出力
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        private static bool ExportSettingFile(List<TargetMachine> list)
        {
            using (var sw = new System.IO.StreamWriter(TargetMachineFileFullPath, false, Encoding))
            {
                //  ヘッダ行
                sw.WriteLine("Host_Name,MAC_Address");

                //  データ行
                foreach (var item in list)
                {
                    sw.WriteLine(item.ToCSV());
                }
            }

            return true;
        }


        /// <summary>
        /// ホスト名からMACアドレスへの変換
        /// </summary>
        /// <param name="devices"></param>
        /// <param name="hostName"></param>
        /// <returns></returns>
        private static string ToMACAddress(IEnumerable<SharpPcap.LibPcap.LibPcapLiveDevice> devices,
                                           string hostName)
        {
            var ips = System.Net.Dns.GetHostAddresses(hostName);
            foreach (var device in devices)
            {
                var arp = new SharpPcap.ARP(device);

                foreach (var ip in ips)
                {
                    var mac = arp.Resolve(System.Net.IPAddress.Parse(ip.ToString()));
                    if (mac != null)
                    {
                        return mac.ToString();
                    }
                }
            }

            return null;
        }


        /// <summary>
        /// 自分の端末のインタフェースのうち、パケット送信ができるものを返す
        /// </summary>
        /// <returns></returns>
        private static List<SharpPcap.LibPcap.LibPcapLiveDevice> CollectSendableDevices()
        {
            //  デフォルトゲートウェイが設定されているものを、パケット送信可能とみなす
            return SharpPcap.LibPcap.LibPcapLiveDeviceList.Instance
                                    .Where(d => d.Interface.GatewayAddress != null)
                                    .ToList();
        }



        /// <summary>
        /// 対象端末
        /// </summary>
        class TargetMachine
        {
            public string HostName { get; set; }
            public string MACAddress { get; set; }

            public static TargetMachine Analyze(string[] fields)
            {
                return new TargetMachine
                {
                    HostName = fields[0],
                    MACAddress = fields[1]
                };
            }


            public string ToCSV()
            {
                return HostName + "," + MACAddress;
            }
        }
    }
}
