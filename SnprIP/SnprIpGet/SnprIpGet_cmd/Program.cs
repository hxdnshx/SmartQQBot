using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

using System.Threading.Tasks;
using System.Net.Security;
using System.Net;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using SnprIpGet;

namespace SnprIpGet_cmd
{
    class Program
    {
        public int HardID;//hard揭示板的id
        void GetHardId()
        {
            HttpWebResponse ret = HttpHelper.CreateGetHttpResponse(@"http://jbbs.shitaraba.net/netgame/14382/", 60, "", null);
            string str = HttpHelper.GetResponseString(ret);
            ///bbs/read.cgi/netgame/14382/1432657204/l50">4</a> : <a rel="nofollow" href="#4">【テンプレ必読】「Hard」ネット対戦スレッド -008-
            MatchCollection Matches = Regex.Matches(str, "/bbs/read.cgi/netgame/14382/([0-9]+)/l50[^【]{1,400}【テンプレ必読】「Lunatic」ネット対戦スレッド");
            //MessageBox.Show(Matches[0].Result("$1"));
            HardID = int.Parse(Matches[0].Result("$1"));
            Console.Write("HardID Get:" + HardID + "\r\n");
        }


        private string GetIPInfo(int talkid)
        {
            HttpWebResponse ret = HttpHelper.CreateGetHttpResponse(@"http://jbbs.shitaraba.net/bbs/rawmode.cgi/netgame/14382/" + HardID + "/" + talkid, 60, "", null);
            string str = HttpHelper.GetResponseString(ret);
            Match Match = Regex.Match(str, @"([0-9]+)<>([^<]*)<>([^<]*)<>([^<]*)<>([^\n]+?)<>([^<]*)<>([^<]*)\n");
            if (Match.Success)
            {
                return GetIPInfo(Match);
            }
            return "";
        }

        private string GetIPInfo(Match info)
        {
            string check;
            Match infomat = Regex.Match(info.Result("$5"), "【IP:Port】([^<]+)<br>[^○]+【使用キャラ】([^<]+)");
            if (infomat.Success)
            {
                check = CheckIPEx(infomat.Result("$1"));
                if (check == "Unavailable") return "";
                return infomat.Result("$1") + " 使用角色:" + infomat.Result("$2") + "  " + check +"\r\n";
            }
            infomat = Regex.Match(info.Result("$5"), "【IP】([^<]+)<br>【Port】([^<]+)<br>[^○]+【使用キャラ】([^<]+)");
            if (infomat.Success)
            {
                check = CheckIPEx(infomat.Result("$1") + ":" + infomat.Result("$2"));
                if (check == "Unavailable") return "";
                return infomat.Result("$1") + ":" + infomat.Result("$2") + " 使用角色:" + infomat.Result("$3") + "  " + check  + "\r\n";
            }
            infomat = Regex.Match(info.Result("$5"), "&gt;&gt;([0-9]+)</a>再募集");
            if (infomat.Success)
            {
                return GetIPInfo(int.Parse(infomat.Result("$1")));
            }
            infomat = Regex.Match(info.Result("$5"), "&gt;&gt;([0-9]+)</a>〆");
            if (infomat.Success)
            {
                return GetIPInfo(int.Parse(infomat.Result("$1")));
            }
            return "";
        }

        public string CheckIPEx(string str)
        {
            int i;
            string ret = "";
            for (i = 0; i < 4; i++)
            {
                if (i > 2)
                    ret = CheckIP_FXTZ(str) + "(则)";
                else
                    ret = CheckIP(str) + "(录)";
                if (!Regex.Match(ret, @"Unavailable\(.+\)").Success)
                {
                    break;
                }
                else
                    ret = "Unavailable";
            }
            return ret;
        }

        public string CheckIP_FXTZ(string str)
        {
            Match mat = Regex.Match(str, "([0-9]+).([0-9]+).([0-9]+).([0-9]+):(.+)");
            byte[] ipArr = new byte[4];
            Random r = new Random();
            int i;
            IPAddress addr;
            try
            {
                for (i = 1; i < 5; ++i)
                {
                    ipArr[i - 1] = byte.Parse(mat.Result("$" + i));
                }
                addr = new IPAddress(ipArr);
            }
            catch (Exception e)
            {
                return "Unavailable";
            }
            int port = int.Parse(mat.Result("$5"));
            UdpClient ptarget;
            try
            {
                ptarget = new UdpClient(r.Next(10000, 30000));
            }
            catch (Exception e)
            {
                return "Unavailable?";
            }
            List<Socket> sk = new List<Socket>();
            Byte[] pingdata = new Byte[37]
		            {
			            0x01, 0x02, 0x00, 0x2a, 0x30, 0x70, 0x6f, 0x63, 0x55, 0x00,
			            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x2a,
			            0x30, 0x70, 0x6f, 0x63, 0x55, 0x00, 0x00, 0x00, 0x00, 0x00,
			            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
		            }; ;

            int before = Environment.TickCount;
            ptarget.Send(pingdata, 37, new IPEndPoint(addr, port));
            sk.Add(ptarget.Client);
            Socket.Select(sk, null, null, (int)2 * 1000);
            int currentdelay = Environment.TickCount - before;
            IPEndPoint ipe = new IPEndPoint(0, 0);
            try
            {
                for (; ; )
                {
                    ptarget.Client.ReceiveTimeout = 150;
                    byte[] rdata = ptarget.Receive(ref ipe);
                    if (rdata.Length == 1 && rdata[0] == 3)
                    {
                        ptarget.Close();
                        return "可连接";
                        //ptarget.Send(querybattle, 110, new IPEndPoint(addr, port));
                        //continue;
                    }

                    break;
                }
            }
            catch (Exception e)
            {
                ptarget.Close();
                return "Unavailable";
            }
            ptarget.Close();
            return "Unavailable";
        }

        public string CheckIP(string str)
        {
            Match mat = Regex.Match(str, "([0-9]+).([0-9]+).([0-9]+).([0-9]+):(.+)");
            byte[] ipArr = new byte[4];
            Random r = new Random();
            int i;
            IPAddress addr;
            try
            {
                for (i = 1; i < 5; ++i)
                {
                    ipArr[i - 1] = byte.Parse(mat.Result("$" + i));
                }
                addr = new IPAddress(ipArr);
            }
            catch(Exception e)
            {
                return "Unavailable";
            }
            int port = int.Parse(mat.Result("$5"));
            UdpClient ptarget;
            try
            {
                ptarget= new UdpClient(r.Next(10000, 30000));
            }
            catch(Exception e)
            {
                return "Unavailable?";
            }
            List<Socket> sk = new List<Socket>();
            Byte[] pingdata = new Byte[24]
            {
                0x08,0x57,0x09,0xf6,0x67,0xf0,0xfd,0x4b,
                0xd0,0xb9,0x9a,0x74,0xf8,0x38,0x33,0x81,
                0x88,0x00,0x00,0x00,0xa4,0x7d,0x12,0x00
            };
            pingdata[20] = (Byte)r.Next(0, 255);
            pingdata[21] = (Byte)r.Next(0, 255);
            pingdata[22] = (Byte)r.Next(0, 255);
            Byte[] querybattle = new Byte[110]
            {
                0x09,0x57,0x09,0xf6,0x67,0xf0,0xfd,0x4b,0xd0,0xb9,0x9a,0x74,0xf8,
                0x38,0x33,0x81,0x88,0x00,0x00,0x00,0x56,0xc2,0x51,0x01,0x00,0x00,
                0x00,0x00,0x00,0x00,0x4a,0x00,0x4a,0x00,0x00,0x01,0x4b,0x00,0x00,
                0x00,0x78,0x9c,0x13,0x60,0x60,0xe0,0xe0,0x60,0x60,0x60,0xc8,0x2c,
                0x8e,0x2f,0x4f,0x2c,0x49,0xce,0x00,0xb2,0x19,0x19,0x05,0x80,0x82,
                0xec,0x40,0xc1,0xb2,0xd4,0xa2,0xe2,0xcc,0xfc,0x3c,0x26,0x06,0x06,
                0x56,0x71,0x15,0x31,0x06,0x90,0x30,0x37,0x50,0x38,0x23,0x33,0x25,
                0x25,0x35,0x2f,0x3e,0x39,0x23,0xb1,0x08,0x24,0x05,0x52,0xc9,0x08,
                0x44,0x00,0xc6,0xa8,0x0b,0x96
            };
            Byte[] disconnect = new byte[12]
            {
                0x10,0x07,0x16,0x01,0xe3,0x80,0x0f,0x00,0x02,0x00,0x00,0x00
            };
            int before = Environment.TickCount;
            ptarget.Send(pingdata, 24, new IPEndPoint(addr, port));
            sk.Add(ptarget.Client);
            Socket.Select(sk, null, null, (int)2 * 1000);
            int currentdelay = Environment.TickCount - before;
            IPEndPoint ipe = new IPEndPoint(0, 0);
            try
            {
                for (; ; )
                {
                    ptarget.Client.ReceiveTimeout = 250;
                    byte[] rdata = ptarget.Receive(ref ipe);
                    if (rdata.Length == 1 && rdata[0] == 4)
                    {
                        //ptarget.Close();
                        //return true;
                        ptarget.Send(querybattle, 110, new IPEndPoint(addr, port));
                        continue;
                    }
                    else if (rdata.Length == 32 && rdata[0] == 7)//Ping包?
                    {
                        //禁止的情况不会有这种ping包
                        ptarget.Send(disconnect, 12, new IPEndPoint(addr, port));
                        ptarget.Close();
                        return "可以观战";
                        //ptarget.Send(rdata, 32, new IPEndPoint(addr, port));
                        continue;
                    }
                    else if (rdata.Length == 32 && rdata[0] == 0x0c)
                    {
                        ptarget.Close();
                        return "没人插入或禁止观战";
                    }
                    else if (rdata.Length == 92 && rdata[0] == 0x0b)
                    {
                        ptarget.Send(disconnect, 12, new IPEndPoint(addr, port));
                        ptarget.Close();
                        return "可以观战";
                    }
                    break;
                }
            }
            catch (Exception e)
            {
                ptarget.Close();
                return "Unavailable";
            }
            ptarget.Close();
            return "Unavailable";
        }

        static void Main(string[] args)
        {
            Program inst=new Program();
            if(args.Length<1)//至少有一个参数
            {
                Console.Write("Using:SnprIpGet_cmd SavePath\r\nOr SnprIpGet_cmd SavePath -t UserName\r\nOr SnprIpGet_cmd SavePath -t IPAddress");
                return;
            }
            if(args.Length==3)
            {
                if(args[1]== "-i")//测试ip
                {
                    File.WriteAllText(args[0], args[2] + "的当前状态:"+ inst.CheckIPEx(args[2]), Encoding.UTF8);
                    return;
                }
                if(args[1]=="-z")//Tenco 战斗力(则)
                {
                    HttpWebResponse ret = HttpHelper.CreateGetHttpResponse(@"http://tenco.info/game/2/account/" + args[2] + "/", 60, "", null);
                    string str = HttpHelper.GetResponseStringRegular(ret);
                    string retstr = "\r\n用户" + args[2] + "的Tenco战斗力(则):";
                    MatchCollection Matches = Regex.Matches(str, "images/game/2[^>]+>([^<]+)</td>[^>]+>([^<]*?戦)</td>[^>]+>([0-9]+)<sub>±([0-9]+)</sub>");
                    if (Matches.Count == 0)
                    {
                        retstr += "\r\n没有找到";
                    }
                    else
                    {
                        foreach (Match mat in Matches)
                        {
                            retstr += "\r\n" + mat.Result("$1") + "  " + mat.Result("$2") + "  " + mat.Result("$3") + "±" + mat.Result("$4");
                        }
                    }
                    File.WriteAllText(args[0], retstr, Encoding.UTF8);
                }
                if(args[1] == "-t")//Tenco 战斗力
                {
                    try
                    {
                        HttpWebResponse ret = HttpHelper.CreateGetHttpResponse(@"http://tenco.info/game/6/account/" + args[2] + "/", 60, "", null);
                        string str = HttpHelper.GetResponseStringRegular(ret);
                        string retstr = "用户" + args[2] + "的Tenco战斗力(录):";
                        MatchCollection Matches = Regex.Matches(str, "images/game/6[^>]+>([^<]+)</td>[^>]+>([^<]*?戦)</td>[^>]+>([0-9]+)<sub>±([0-9]+)</sub>");
                        if (Matches.Count == 0)
                        {
                            retstr += "\r\n没有找到";
                        }
                        else
                        {
                            foreach (Match mat in Matches)
                            {
                                retstr += "\r\n" + mat.Result("$1") + "  " + mat.Result("$2") + "  " + mat.Result("$3") + "±" + mat.Result("$4");
                            }
                        }
                        
                        File.WriteAllText(args[0], retstr, Encoding.UTF8);
                    }
                    catch(System.Net.WebException)
                    {
                        File.WriteAllText(args[0], "错误!未找到用户" + args[2], Encoding.UTF8);
                        return;
                    }
                    return;
                }
            }
            inst.GetHardId();
            bool updateflag=false;
            int ti;
            for(;;)
            {
                if(updateflag)
                {
                    inst.GetHardId();
                    updateflag = false;
                }
                HttpWebResponse ret = HttpHelper.CreateGetHttpResponse(@"http://jbbs.shitaraba.net/bbs/rawmode.cgi/netgame/14382/" + inst.HardID + "/l20", 60, "", null);
                string str = HttpHelper.GetResponseString(ret);
                string retstr = "当前揭示板Lunatic难度IP:(更新时间:" + DateTime.Now.ToString() + ")\r\n";
                MatchCollection Matches = Regex.Matches(str, @"([0-9]+)<>([^<]*)<>([^<]*)<>([^<]*)<>([^\n]+?)<>([^<]*)<>([^<]*)\n");
                foreach (Match mat in Matches)
                {
                    ti=int.Parse(mat.Result("$1"));
                    if (ti > 990) updateflag = true;
                    if (ti != 1)
                    {
                        retstr += inst.GetIPInfo(mat);
                    }

                }
                ret = HttpHelper.CreateGetHttpResponse(@"https://twitter.com/hashtag/th145", 60, "", null);
                str = HttpHelper.GetResponseStringRegular(ret);
                retstr += "推上最新ip:";
                int i = 0;
                string check;

                Matches = Regex.Matches(str, "with-id\" data-aria-label-part>([^<]+)[.\\s\\S]*?last\">([^<]+)[.\\s\\S]*?([0-9]+\\.[0-9]+\\.[0-9]+\\.[0-9]+:[0-9]+)");//"<small class=\"time\">.*?title=\"([^\"]+)\".*?([0-9]+\\.[0-9]+\\.[0-9]+\\.[0-9]+:[0-9])");
                foreach (Match mat in Matches)
                {
                    check = inst.CheckIPEx(mat.Result("$3"));
                    if (check == "Unavailable") continue;
                    retstr += "\r\n" + mat.Result("$1") + " - " + mat.Result("$3") + "  " + check + "   " + mat.Result("$2");
                    i++;
                    if (i == 5) break;
                }
                File.WriteAllText(args[0], retstr, Encoding.UTF8);
                Console.Write(retstr);
                Console.Write("\r\nDataUpdated\r\n");
                System.Threading.Thread.Sleep(300000);//休息5分钟
            }
        }
    }
}
