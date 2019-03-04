using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;

namespace Chemical_HttpServer
{
    class Program
    {
        static Dictionary<string, int> ContinueOffset;
        static Dictionary<string, string> ContinuingIp;
        static string connetStr = "server=192.168.50.53;port=3306;user=root;password=wuxiaohan; database=chemical;";
        static HttpListener httpobj;
        static string HttpContentDir = "D:/HttpContent/apache/Apache24/htdocs/boundles/";
        static string HttpContentRootUrl = "192.168.50.53:5757";
        static void Main(string[] args)
        {
            ContinueOffset = new Dictionary<string, int>();
            ContinuingIp = new Dictionary<string, string>();
            MySqlInit();
            //提供一个简单的、可通过编程方式控制的 HTTP 协议侦听器。此类不能被继承。
            httpobj = new HttpListener();
            //定义url及端口号，通常设置为配置文件
            httpobj.Prefixes.Add("http://192.168.50.53:5656/");
            //启动监听器
            httpobj.Start();
            //异步监听客户端请求，当客户端的网络请求到来时会自动执行Result委托
            //该委托没有返回值，有一个IAsyncResult接口的参数，可通过该参数获取context对象
            httpobj.BeginGetContext(new AsyncCallback(Result), httpobj);
            // httpobj.BeginGetContext(Result, null);
            Console.WriteLine($"服务端初始化完毕，正在等待客户端请求,时间：{DateTime.Now.ToString()}\r\n");
            Console.ReadKey();
        }

        static void MySqlInit()
        {
            MySqlConnection conn;
            conn = new MySqlConnection(connetStr);

            var sqlre = true;
            try
            {
                conn.Open();
                Console.WriteLine("Connected!");
            }
            catch (MySqlException ex)
            {
                Console.WriteLine(ex.Message);
                sqlre = false;
            }
            finally
            {
                conn.Close();

            }
            if (!sqlre)
            {
                Console.ReadKey();
                // return;
            }
        }
        private static void Result(IAsyncResult ar)
        {
            //当接收到请求后程序流会走到这里

            //继续异步监听
            httpobj.BeginGetContext(Result, null);
            var guid = Guid.NewGuid().ToString();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"接到新的请求:{guid},时间：{DateTime.Now.ToString()}");
            //获得context对象
            var context = httpobj.EndGetContext(ar);
            var request = context.Request;
            var response = context.Response;
            ////如果是js的ajax请求，还可以设置跨域的ip地址与参数
            //context.Response.AppendHeader("Access-Control-Allow-Origin", "*");//后台跨域请求，通常设置为配置文件
            //context.Response.AppendHeader("Access-Control-Allow-Headers", "ID,PW");//后台跨域参数设置，通常设置为配置文件
            //context.Response.AppendHeader("Access-Control-Allow-Method", "post");//后台跨域请求设置，通常设置为配置文件
            context.Response.ContentType = "text/plain;charset=UTF-8";//告诉客户端返回的ContentType类型为纯文本格式，编码为UTF-8
            context.Response.AddHeader("Content-type", "text/plain");//添加响应头信息
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            //context.Response.Headers.Add("Access-Control-Allow-Methods", "POST");
            //context.Response.Headers.Add("Access-Control-Allow-Headers", "x-requested-with,content-type");

            string returnObj = null;//定义返回客户端的信息
            if (request.HttpMethod == "POST" && request.InputStream != null)
            {

                Dictionary<string, string> _params;
                GetParams(request.Url.Query, out _params);
                if (_params == null)
                {
                    returnObj = MakeSampleReturnJson(new string[] { "type", "result", "code" }, new string[] { "error", "can not find any params", "200" });
                    return;
                }
                var postMethod = _params["method"];
                if (postMethod == "register")
                {
                    var userName = _params["userName"];
                    var password = _params["password"];
                    string[] cols = { "userName", "password" };
                    string[] values = { userName, password };
                    bool isExist = MySqlIsExist("chemical", "users", "userName", userName);
                    if (isExist)
                    {
                        returnObj = MakeSampleReturnJson(new string[] { "type", "result", "code" }, new string[] { "error", "the same userName exist", "200" });


                    }
                    else
                    {
                        int re = MySqlInsert("chemical", "users", cols, values);
                        returnObj = MakeSampleReturnJson(new string[] { "type", "result", "code" }, new string[] { "normal", "OK", "200" });

                    }


                    //  MySqlInsert("chemical", "users", cols, values);




                }
                else if (postMethod == "login")
                {
                    var userName = _params["userName"];
                    var password = _params["password"];
                    string[] cols = { "userName", "password" };
                    string[] values = { userName, password };
                    bool isExist = MySqlIsExist("chemical", "users", "userName", userName);
                    if (!isExist)
                    {
                        returnObj = MakeSampleReturnJson(new string[] { "type", "result", "code" }, new string[] { "error", "userName does not exist", "200" });

                    }
                    else
                    {
                        //string[] tmpcol = { "userName" };
                        MySqlConnection conn = null;
                        string restr = null;
                        var reader = MySqlQuery("chemical", "users", cols, "userName", userName, out conn, out restr);
                        if (reader == null)
                        {
                            returnObj = MakeSampleReturnJson(new string[] { "type", "result", "code" }, new string[] { "error", restr, "200" });
                            goto LoginOut;
                        }
                        reader.Read();
                        if (reader[1].ToString().CompareTo(password) == 0)
                        {
                            returnObj = MakeSampleReturnJson(new string[] { "type", "result", "code" }, new string[] { "normal", "OK", "200" });
                        }
                        else
                        {
                            returnObj = MakeSampleReturnJson(new string[] { "type", "result", "code" }, new string[] { "error", "wrong password", "200" });
                        }
                        conn.Close();
                        conn = null;
                    }
                    LoginOut:;
                }
                else if (postMethod == "adminLogin")
                {
                    #region DebugCode
                    //debug code
                    var userName = _params["userName"];
                    var password = _params["password"];
                    if (userName == "admin" && password == "admin")
                        returnObj = "OK";
                    else
                        returnObj = "0";
                    //end debug
                    #endregion
                }
                else if (postMethod == "adminEditTable")
                {
                    string re = ReadInputStreamToString(request.InputStream, int.Parse(request.Headers.GetValues("Content-Length")[0]));
                    if (string.IsNullOrEmpty(re))
                    {
                        returnObj = MakeSampleReturnJson(new string[] { "type", "result", "code" }, new string[] { "error", "post data is null!", "200" });
                    }
                    else
                    {
                        JObject jobj = JObject.Parse(re);
                        JArray jarr = JArray.Parse(jobj["obj"].ToString());
                        foreach (var i in jarr)
                        {
                            JObject tmpjobj = JObject.Parse(i.ToString());
                            var property = tmpjobj.Properties();
                            List<string> keys = new List<string>();
                            List<string> values = new List<string>();
                            foreach (var j in property)
                            {
                                keys.Add(j.Name);
                                values.Add(j.Value.ToString());
                            }
                            bool mysqlRe = MySqlEdit("chemical", _params["tableName"], _params["id"], keys.ToArray(), values.ToArray());
                            if (mysqlRe)
                                returnObj = MakeSampleReturnJson(new string[] { "type", "result", "code" }, new string[] { "normal", "OK", "200" });

                            else
                                returnObj = MakeSampleReturnJson(new string[] { "type", "result", "code" }, new string[] { "warning", "edit table faild", "200" });
                        }
                        //   
                    }
                    //MySqlEdit("chemical","users",)
                }
                else if (postMethod == "adminDeleteItem")
                {
                    string re = ReadInputStreamToString(request.InputStream, int.Parse(request.Headers.GetValues("Content-Length")[0]));
                    if (string.IsNullOrEmpty(re))
                    {
                        returnObj = MakeSampleReturnJson(new string[] { "type", "result", "code" }, new string[] { "error", "json null", "200" });

                    }
                    else
                    {
                        if (_params.Count > 2)
                        {
                            MySqlDelete("chemical", _params["tableName"], new string[] { _params["id"] });
                        }
                        else
                        {
                            JObject root = JObject.Parse(re);
                            JArray jarr = JArray.Parse(root["objs"].ToString());
                            List<string> ids = new List<string>();
                            foreach (var i in jarr)
                            {
                                ids.Add(i.ToString());
                            }
                            MySqlDelete("chemical", _params["tableName"], ids.ToArray());
                        }
                        returnObj = MakeSampleReturnJson(new string[] { "type", "result", "code" }, new string[] { "normal", "OK", "200" });
                    }
                }
                else if (postMethod == "adminMutilpleInsert")
                {

                }
                else if (postMethod == "upload")
                {
                    var fileName = _params["fileName"];
                    if (!_params.ContainsKey("dataContinue"))
                    {
                        returnObj = MakeSampleReturnJson(new string[] { "type", "result", "code" }, new string[] { "warning", "Http params incomplete!", "200" });
                    }
                    var realFileName = HttpContentDir + fileName;
                    FileInfo fi = new FileInfo(realFileName);
                    if (fi.Exists)
                    {
                        if (_params["dataContinue"] == "false")
                        {
                            if (ContinueOffset.ContainsKey(realFileName))//exist and continuing
                            {
                                returnObj = MakeSampleReturnJson(new string[] { "type", "result", "code" }, new string[] { "error", "file are continuing", "200" });

                            }
                            else//exist and replace
                            {
                                returnObj = MakeSampleReturnJson(new string[] { "type", "result", "code" }, new string[] { "warning", "file exist.Do you want to replace this file", "200" });
                            }
                        }
                        else
                        {
                            string ip = request.RemoteEndPoint.Address.ToString();
                            if (ContinuingIp.ContainsKey(realFileName))
                            {
                                if(ContinuingIp[realFileName] == ip)//continue file upload (seek)
                                {
                                    returnObj = ReadInputStream(request.InputStream, realFileName, _params["dataContinue"]);
                                }
                                else//continue file but ip does not matching
                                {
                                    returnObj = MakeSampleReturnJson(new string[] { "type", "result", "code" }, new string[] { "error", "other user are continuing upload file", "200" });
                                }
                            }
                            else//continue file first time upload(seek 0)
                            {
                                ContinuingIp.Add(realFileName, ip);
                                returnObj = ReadInputStream(request.InputStream, realFileName, _params["dataContinue"]);
                            }
                        }
                    }
                    else
                    {
                        //normal upload
                        returnObj = ReadInputStream(request.InputStream, realFileName, _params["dataContinue"]);
                    }
                }
                else if (postMethod == "forceUpload")
                {
                    var userName = _params["userName"];
                    var fileName = _params["fileName"];
                    //var fileSize = _params["fileSize"];
                    var realFileName = HttpContentDir + fileName;
                    if (_params.ContainsKey("dataContinue"))
                    {
                        returnObj = ReadInputStream(request.InputStream, realFileName, _params["dataContinue"]);
                    }
                    else
                    {
                        returnObj = MakeSampleReturnJson(new string[] { "type", "result", "code" }, new string[] { "warning", "Http params incomplete!", "200" });
                    }
                }

                // returnObj = $"POST";//HandleRequest(request, response);
            }
            #region GET
            else if (request.HttpMethod == "GET")
            {
                Dictionary<string, string> _params;
                GetParams(request.Url.Query, out _params);
                var getMethod = _params["method"];
                if (getMethod == "download")
                {
                    //var userName = _params["userName"];
                    var fileName = _params["fileName"];
                    var realFileName = HttpContentDir + fileName;
                    byte[] data = null;
                    bool s = false;
                    string restr = ReadLocalFile(realFileName, out data, out s);
                    if (s)
                    {
                        //   response.OutputStream.Write(data, 0, data.Length);
                        returnObj = MakeSampleReturnJson(new string[] { "type", "result", "code", "url" }, new string[] { "normal", "OK", "200", HttpContentRootUrl + "/boundles/" + fileName });

                    }
                    else
                    {
                        returnObj = MakeSampleReturnJson(new string[] { "type", "result", "code" }, new string[] { "error", restr, "200" });

                    }
                }
                else if (getMethod == "adminGetTable")
                {
                    var tableName = _params["tableName"];
                    MySqlConnection conn = null;
                    string restr = null;
                    var reader = MySqlQuery("chemical", tableName, new string[] { "*" }, null, null, out conn, out restr);
                    if (reader == null)
                    {
                        returnObj = MakeSampleReturnJson(new string[] { "type", "result", "code" }, new string[] { "error", restr, "200" });
                        goto AdminGetTableOut;
                    }
                    string json = "{\"type\":\"normal\",\"result\":\"" + "OK" + "\",\"code\":\"200\"," + "\"objs\":[";
                    bool isnull = true;
                    while (reader.Read())
                    {
                        isnull = false;
                        json = json + "{";
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            json = json + "\"" + i.ToString() + "\":\"" + reader[i].ToString() + "\"";
                            if (i != reader.FieldCount - 1)
                            {
                                json = json + ",";
                            }
                        }
                        json = json + "},";
                    }
                    if (isnull)
                    {
                        returnObj = MakeSampleReturnJson(new string[] { "type", "result", "code" }, new string[] { "warning", "Warning: NullTable!", "200" });

                    }
                    else
                    {
                        json = json.Remove(json.Length - 1, 1);
                        json = json + "]}";
                        returnObj = json;
                    }
                    conn.Close();
                    AdminGetTableOut:;
                }

            }
            else
            {

                returnObj = $"404";
            }
            #endregion
            #region fix

            var returnByteArr = Encoding.UTF8.GetBytes(returnObj);//设置客户端返回信息的编码
            try
            {
                using (var stream = response.OutputStream)
                {
                    //把处理信息返回到客户端
                    stream.Write(returnByteArr, 0, returnByteArr.Length);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"网络蹦了：{ex.ToString()}");
            }
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"请求处理完成：{guid},时间：{ DateTime.Now.ToString()},返回信息:{returnObj}\r\n");
            #endregion
        }
        #region sql
        static int MySqlInsert(string dataBase, string tableName, string[] colNames, string[] values)
        {
            string command = "INSERT INTO " + dataBase + "." + tableName + " (";
            for (int i = 0; i < colNames.Length; i++)
            {
                command = command + colNames[i];
                if (i != colNames.Length - 1)
                {
                    command = command + ",";
                }
            }
            command = command + ") value (";
            for (int i = 0; i < values.Length; i++)
            {
                command = command + "\"" + values[i] + "\"";
                if (i != values.Length - 1)
                {
                    command = command + ",";
                }
            }
            command = command + ");";
            using (var conn = new MySqlConnection(connetStr))
            {
                conn.Open();
                var comm = new MySqlCommand(command, conn);
                int re = comm.ExecuteNonQuery();
                return re;
            }
        }

        static MySqlDataReader MySqlQuery(string dataBase, string tableName, string[] colNames, string where, string whereValue, out MySqlConnection conn, out string reStr)
        {
            MySqlDataReader reader = null;
            conn = null;
            try
            {
                string command = "select ";
                for (int i = 0; i < colNames.Length; i++)
                {
                    command = command + colNames[i];
                    if (i != colNames.Length - 1)
                    {
                        command = command + ",";
                    }
                    else
                    {
                        command = command + " ";
                    }
                }
                command = command + " from " + dataBase + "." + tableName;
                if (where != null)
                    command = command + " where " + where + "=\"" + whereValue + "\"";
                conn = new MySqlConnection(connetStr);
                conn.Open();
                MySqlCommand CMD = new MySqlCommand(command, conn);

                reader = CMD.ExecuteReader();
                reStr = "OK";
                return reader;
            }
            catch (Exception ex)
            {
                if (conn != null)
                {
                    conn.Close();
                }
                reStr = ex.Message;
                reader = null;
                return reader;
            }
        }

        static bool MySqlIsExist(string dataBase, string tableName, string colNames, string specificValue)
        {
            string command = "select " + colNames;

            command = command + " from " + dataBase + "." + tableName + " where " + colNames + "=\"" + specificValue + "\"";
            using (var conn = new MySqlConnection(connetStr))
            {
                conn.Open();
                MySqlCommand CMD = new MySqlCommand(command, conn);
                MySqlDataReader reader = null;
                reader = CMD.ExecuteReader();
                bool isExist = false;
                while (reader.Read())
                {
                    isExist = true;
                }
                return isExist;
            }
        }
        static bool MySqlEdit(string dataBase, string tableName, string id, string[] keys, string[] values)
        {
            if (keys.Length != values.Length)
            {
                return false;
            }
            string command = "UPDATE " + dataBase + "." + tableName + " SET ";
            int index = 0;
            string tmp = "";
            foreach (var i in keys)
            {
                tmp = tmp + i + " = " + "\"" + values[index] + "\"";
                if (index != values.Length - 1)
                {
                    tmp = tmp + ",";
                }
                index++;
            }
            command = command + tmp + " WHERE id = " + id;
            using (var conn = new MySqlConnection(connetStr))
            {
                conn.Open();
                MySqlCommand CMD = new MySqlCommand(command, conn);
                CMD.ExecuteNonQuery();
            }
            return true;
        }

        private static bool MySqlDelete(string dataBase, string tableName, string[] id)
        {
            string command = "DELETE FROM " + dataBase + "." + tableName + " WHERE id in(";
            int index = 0;
            foreach (var i in id)
            {
                command = command + i;
                if (index != id.Length - 1)
                {
                    command = command + ",";
                }
                index++;
            }
            command = command + ")";
            using (var conn = new MySqlConnection(connetStr))
            {
                conn.Open();
                MySqlCommand CMD = new MySqlCommand(command, conn);
                CMD.ExecuteNonQuery();
            }
            return true;
        }
        #endregion
        private static void GetParams(string query, out Dictionary<string, string> _params)
        {
            if (string.IsNullOrEmpty(query))
            {
                _params = null;
                return;
            }
            _params = new Dictionary<string, string>();
            string str = query.Remove(0, 1);
            var parts = str.Split('&');
            foreach (var i in parts)
            {
                string[] single = i.Split('=');
                string key = single[0];
                string value = single[1];
                _params.Add(key, value);

            }
        }

        private static string ReadInputStreamToString(Stream inputStream, int length)
        {

            {
                long logLength = Convert.ToInt64(length);

                byte[] buffer = new byte[logLength];
                //读取用户发送过来的正文
                int jsonLength = inputStream.Read(buffer, 0, buffer.Length);

                if (jsonLength <= 0) return "";

                //将其转为字符串
                string json = Encoding.UTF8.GetString(buffer, 0, jsonLength);
                return json;
            }
        }
        // Dictionary<string, Stream> ContinueStream;
        // private static string ReadInputStream(Stream inputStream, string fileName, string continueFlag)
        // {
        //     byte[] data = null;
        //
        //     try
        //     {
        //         var byteList = new List<byte>();
        //         var byteArr = new byte[2048];
        //         int readLen = 0;
        //         int len = 0;
        //         if (continueFlag == "true")
        //         {
        //             var TmpStream = new BinaryReader(inputStream);
        //             do
        //             {
        //                 readLen = TmpStream.Read(byteArr, 0, byteArr.Length);
        //                 len += readLen;
        //                 byteList.AddRange(byteArr);
        //             } while (readLen != 0);
        //             if (len == 0)
        //             {
        //                 data = null;
        //                 inputStream.Close();
        //                 return MakeSampleReturnJson(new string[] { "type", "result", "code" }, new string[] { "error", "file size 0 byte", "200" });
        //             }
        //             data = byteList.ToArray();
        //             inputStream.Close();
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         inputStream.Close();
        //         Console.ForegroundColor = ConsoleColor.Red;
        //         Console.WriteLine($"在接收数据时发生错误:{ex.ToString()}");
        //         return MakeSampleReturnJson(new string[] { "type", "result", "code" }, new string[] { "error", ex.ToString(), "404" });//把服务端错误信息直接返回可能会导致信息不安全，此处仅供参考
        //     }
        //     if (!ByteToFile(data, fileName))
        //     {
        //         return MakeSampleReturnJson(new string[] { "type", "result", "code" }, new string[] { "error", "save file faild", "200" });
        //
        //     }
        //     else
        //     {
        //         return MakeSampleReturnJson(new string[] { "type", "result", "code" }, new string[] { "normal", "OK", "200" });
        //     }
        //
        // }

        private static string ReadInputStream(Stream inputStream, string fileName, string continueFlag)
        {
            byte[] data = null;
            try
            {
                var byteList = new List<byte>();
                var byteArr = new byte[2048];
                int readLen = 0;
                int len = 0;
                do
                {
                    readLen = inputStream.Read(byteArr, 0, byteArr.Length);
                    if (readLen != 0)
                    {
                        len += readLen;
                        byteList.AddRange(byteArr);
                        if (readLen < 2048)
                        {
                            byteList.RemoveRange(readLen, 2048 - readLen);
                        }
                        
                    }
                } while (readLen != 0);
                if (len == 0)
                {
                    data = null;
                    inputStream.Close();
                    return MakeSampleReturnJson(new string[] { "type", "result", "code" }, new string[] { "error", "file size 0 byte", "200" });
                }
                data = byteList.ToArray();
                inputStream.Close();
            }
            catch (Exception ex)
            {
                inputStream.Close();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"在接收数据时发生错误:{ex.ToString()}");
                return MakeSampleReturnJson(new string[] { "type", "result", "code" }, new string[] { "error", ex.ToString(), "404" });//把服务端错误信息直接返回可能会导致信息不安全，此处仅供参考
            }
            if (!ByteToFile(data, fileName, continueFlag))
            {
                return MakeSampleReturnJson(new string[] { "type", "result", "code" }, new string[] { "error", "save file faild", "200" });

            }
            else
            {
                return MakeSampleReturnJson(new string[] { "type", "result", "code" }, new string[] { "normal", continueFlag, "200" });
            }
        }
        private static string ReadLocalFile(string fileName, out byte[] bytes, out bool s)
        {
            FileInfo fi = new FileInfo(fileName);
            if (!fi.Exists)
            {
                bytes = null;
                s = false;

                return "file does not exist";
            }
            byte[] data = null;
            FileStream fs = new FileStream(fileName, FileMode.Open);
            try
            {
                var byteList = new List<byte>();
                var byteArr = new byte[2048];
                int readLen = 0;
                int len = 0;

                do
                {
                    readLen = fs.Read(byteArr, 0, byteArr.Length);
                    len += readLen;
                    byteList.AddRange(byteArr);
                } while (readLen != 0);
                data = byteList.ToArray();
                fs.Close();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine($"read error:{ex.ToString()}");
                fs.Close();
                bytes = null;
                s = false;
                return "read file error";
            }
            bytes = data;
            s = true;
            return "OK";
        }


        static bool ByteToFile(byte[] byteArray, string fileName, string continueFlag,bool isBinary = true)
        {
            bool result = false;
            try
            {
                using (BinaryWriter fs = new BinaryWriter(new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write)))
                {
                    if (continueFlag == "false")
                    {
                        fs.Write(byteArray, 0, byteArray.Length);
                        result = true;
                    }
                    else if (continueFlag == "true")
                    {
                        if (ContinueOffset.ContainsKey(fileName))
                        {
                            int offset = ContinueOffset[fileName];
                            fs.Seek(0, SeekOrigin.End);
                            fs.Write(byteArray, 0, byteArray.Length);
                            ContinueOffset[fileName] = offset + byteArray.Length;
                            result = true;
                        }
                        else
                        {
                            fs.Write(byteArray, 0, byteArray.Length);
                            ContinueOffset.Add(fileName, byteArray.Length);
                            result = true;
                        }

                    }
                    else//end
                    {
                        int offset = ContinueOffset[fileName];
                        fs.Seek(0, SeekOrigin.End);
                        fs.Write(byteArray, 0, byteArray.Length);
                        ContinueOffset.Remove(fileName);
                        ContinuingIp.Remove(fileName);
                        result = true;
                    }

                    fs.Flush();
                    fs.Close();
                }
            }
            catch (Exception ex)
            {
                result = false;
            }
            return result;
        }

        static string MakeSampleReturnJson(string[] keys, string[] values)
        {
            if (keys.Length != values.Length)
                return "";
            JObject jobj = new JObject();
            for (int i = 0; i < keys.Length; i++)
            {
                jobj[keys[i]] = values[i];
            }
            var jsonStr = jobj.ToString();
            jsonStr = jsonStr.Replace(" ", "");
            return jsonStr;
        }

        //private static string HandleRequest(HttpListenerRequest request, HttpListenerResponse response)
        //{
        //    string data = null;
        //    try
        //    {
        //        var byteList = new List<byte>();
        //        var byteArr = new byte[2048];
        //        int readLen = 0;
        //        int len = 0;
        //        //接收客户端传过来的数据并转成字符串类型
        //        do
        //        {
        //            readLen = request.InputStream.Read(byteArr, 0, byteArr.Length);
        //            len += readLen;
        //            byteList.AddRange(byteArr);
        //        } while (readLen != 0);
        //        data = Encoding.UTF8.GetString(byteList.ToArray(), 0, len);

        //        //获取得到数据data可以进行其他操作
        //    }
        //    catch (Exception ex)
        //    {
        //        response.StatusDescription = "404";
        //        response.StatusCode = 404;
        //        Console.ForegroundColor = ConsoleColor.Red;
        //        Console.WriteLine($"在接收数据时发生错误:{ex.ToString()}");
        //        return $"在接收数据时发生错误:{ex.ToString()}";//把服务端错误信息直接返回可能会导致信息不安全，此处仅供参考
        //    }
        //    response.StatusDescription = "200";//获取或设置返回给客户端的 HTTP 状态代码的文本说明。
        //    response.StatusCode = 200;// 获取或设置返回给客户端的 HTTP 状态代码。
        //    Console.ForegroundColor = ConsoleColor.Green;
        //    Console.WriteLine($"接收数据完成:{data.Trim()},时间：{DateTime.Now.ToString()}");
        //    return $"接收数据完成";
        //}
    }
}
