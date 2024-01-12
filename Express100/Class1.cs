using Kingdee.BOS.App;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;


namespace Express100
{
    [Description("物流100接口")]
    [HotUpdate]
    public class Class1 : AbstractBillPlugIn
    {
        internal static MD5 md5 = new MD5CryptoServiceProvider();
        static long appid = 1683505329;//测试1679240590
        static string appKey = "63509e262210f73a0452ce7b15442a1e";//测试0b99a253019afa0a953a03a3c12af03a
        static string business_id = "11490113630";//测试企业id1161885087
        static string gaodeUrl = "https://restapi.amap.com/v3/geocode/geo?key=3e84ce00e00c442273eae229582c29c0&address=";
        public override void AfterDoOperation(AfterDoOperationEventArgs e)
        {
            base.AfterDoOperation(e);

            switch (e.Operation.Operation.ToUpperInvariant())
            {
                case "TEST":
                    
                    var kuaidibillceshi = FormMetaDataCache.GetCachedFormMetaData(this.Context, "SAL_KuaidiBill");
                    DynamicObject dynObjceshi = new DynamicObject(kuaidibillceshi.BusinessInfo.GetDynamicObjectType());

                    //dynObj["FKuaidiCompany"] = kd;//快递公司


                    dynObjceshi["FKuaidiNum"] = "123";
                    dynObjceshi["FReceiver"] = "收件人";//收件人
                    dynObjceshi["FReceiverPhone"] = 13165654149;//收件人手机
                    dynObjceshi["FToAddress"] = "北京市海淀区学清嘉创大厦A座15层";//收件人地址
                    dynObjceshi["FSender"] = "寄件人";//寄件人
                    dynObjceshi["FSenderPhone"] = 13165654148;//寄件人手机
                    dynObjceshi["FFromAddress"] = "北京市海淀区学清嘉创大厦A座15层2";//寄件人地址
                    dynObjceshi["FCount"] = 2;//商品数量
                    dynObjceshi["FSourceOrderNO"] = "source123";//关联单据编号
                    dynObjceshi["FTimestamp"] = Convert.ToDateTime(TimeServiceHelper.GetSystemDateTime(this.Context).ToString("yyyy-MM-dd HH:mm:ss"));//关联单据编号
                    dynObjceshi["FCreateDate"] = Convert.ToDateTime(TimeServiceHelper.GetSystemDateTime(this.Context).ToString("yyyy-MM-dd HH:mm:ss"));//关联单据编号


                    // 提交数据库保存，并获取保存结果

                    var saveResultceshi = BusinessDataServiceHelper.Save(this.Context, kuaidibillceshi.BusinessInfo, new DynamicObject[] { dynObjceshi });
                    if (saveResultceshi.IsSuccess)
                    {
                        string strsql = string.Format(@"
                            /*dialect*/ 
                            IF NOT EXISTS(SELECT 1 FROM T_SAL_DELIVERYNOTICETRACE WHERE FID = {0} and FCARRYBILLNO = '{1}')
                            BEGIN
                                INSERT INTO T_SAL_DELIVERYNOTICETRACE(FID, FENTRYID, FLOGCOMID, FCARRYBILLNO, FPHONENUMBER)
                                VALUES({0}, (select max(FENTRYID) + 1 from T_SAL_DELIVERYNOTICETRACE), {3},'{1}', {2});
                            END
                            ELSE
                            BEGIN
                                UPDATE T_SAL_DELIVERYNOTICETRACE SET FLOGCOMID = {3}, FPHONENUMBER = {2} WHERE FID = {0} AND FCARRYBILLNO = '{1}';
                            END; 
                            ", 100003, "WULIUDANHAO",131656541492,10001);//单据id，物流单号，手机号，物流公司

                        DBServiceHelper.Execute(this.Context, strsql);

                    }
                    else {
                        List<ValidationErrorInfo> error = saveResultceshi.ValidationErrors;
                        Log.Write("保存电子面单列表失败:" + error.ToString());
                    }
                    
                    break;

                case "GETKUAIDIBILL"://申请电子面单GetKuaidiBill
                    long timestamp = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

                    string url = "https://openic.sf-express.com/open/api/external/createorder4c?sign=";
                    long entity = this.Model.GetEntryRowCount("FKuaidiEntity");

                    /*
                    for (int i = 10; i < entity; i++)
                    {
                        DynamicObject kd = this.Model.GetValue("FKuaidiCompany", i) as DynamicObject;
                        //string ceshi=kd["Name"].ToString();

                        JObject postData = new JObject();
                        postData.Add("dev_id", 1679240590);//同城开发者ID
                        postData.Add("push_time", timestamp);//推单时间
                        postData.Add("user_order_id", "127851123ppa");//商家订单号

                        postData.Add("company_id", "1161885087");//企业ID
                        //postData.Add("shop_id", 3243279847393);//店铺ID


                        postData.Add("settlement_type", 3);//结算方式，1同城月结 3大网月结 4余额支付
                        postData.Add("city_name", "北京");//发单城市


                        postData.Add("account_phone", 13203559287);//下单人手机号
                        postData.Add("customer_code", 5717110613);//大网月结卡号


                        JObject sender = new JObject();//发件人信息
                        sender.Add("sender_name", "顺丰同城");//发件人姓名
                        sender.Add("sender_phone", "13881979410");//发件人电话
                        sender.Add("sender_address", "北京市海淀区学清嘉创大厦A座15层");//发件人地址
                        sender.Add("sender_lng", 116.377333);//发件人经度
                        sender.Add("sender_lat", 40.032412);//发件人纬度


                        JObject receive = new JObject();//收件人信息
                        receive.Add("receiver_name", "顺丰同城");//收件人姓名
                        receive.Add("receiver_phone", "18968188497");//收件人手机
                        receive.Add("receiver_address", "北京市海淀区学清嘉创大厦A座15层");//收件人地址
                        String gaoderesult = SendGetRequest(gaodeUrl + "北京市海淀区学清嘉创大厦A座15层");
                        JObject gaodejson = JObject.Parse(gaoderesult);
                        if ((string)gaodejson["status"] == "1")
                        {
                            string location = gaodejson["geocodes"][0]["location"].ToString();
                            string[] parts = location.Split(',');
                            string jd = parts[0];
                            string wd = parts[1];
                            receive.Add("receiver_lng", double.Parse(jd));//收件人经度
                            receive.Add("receiver_lat", double.Parse(wd));//纬度
                        }
                        else
                        {
                            throw new Exception("高德接口异常：" + gaodejson["info"].ToString());
                        }

                        JObject order_detail = new JObject();//物品信息
                        order_detail.Add("total_price", 500); //金额    100 表示1元
                        order_detail.Add("product_type", 16); //类型

                        postData.Add("sender", sender);
                        postData.Add("receiver", receive);
                        postData.Add("order_detail", order_detail);


                        String sign = generateOpenSign(postData.ToString(), 1679240590, "0b99a253019afa0a953a03a3c12af03a");

                        Log.Write("url+sign：" + url + sign + "----postData:" + postData.ToString());


                        string backResult = Post(url + sign, postData.ToString());


                        JObject jsonObject = JObject.Parse(backResult);
                        if ((int)jsonObject["error_code"] == 0)
                        {
                            string sfno = jsonObject["result"]["sf_order_id"].ToString();
                            this.Model.SetValue("FKuaidiNum", sfno, i);
                            Log.Write("顺丰单号：" + sfno);

                            
                        }

                        var kuaidibill = FormMetaDataCache.GetCachedFormMetaData(this.Context, "SAL_KuaidiBill");
                        var metaData = MetaDataServiceHelper.GetFormMetaData(this.Context, "SAL_KuaidiBill");



                        DynamicObject dynObj = new DynamicObject(kuaidibill.BusinessInfo.GetDynamicObjectType());


                        dynObj["FKuaidiCompany"] = kd;//快递公司
                        dynObj["FKuaidiNum"] = "123";
                        dynObj["FReceiver"] = "收件人";//收件人
                        dynObj["FReceiverPhone"] = 13165654149;//收件人手机
                        dynObj["FToAddress"] = "北京市海淀区学清嘉创大厦A座15层";//收件人地址
                        dynObj["FSender"] = "寄件人";//寄件人
                        dynObj["FSenderPhone"] = 13165654148;//寄件人手机
                        dynObj["FFromAddress"] = "北京市海淀区学清嘉创大厦A座15层2";//寄件人地址
                        dynObj["FSourceOrderNO"] = this.Model.GetValue("FSourceOrderNO", i).ToString();//关联单据编号


                        // 提交数据库保存，并获取保存结果

                        var saveResult = BusinessDataServiceHelper.Save(this.Context, metaData.BusinessInfo, new DynamicObject[] { dynObj });


                    }
                    */

                    for (int i = 0; i < entity; i++) {

                        try
                        {
                            Log.Write("进入api接口");
                            DynamicObject kd=this.Model.GetValue("FKuaidiCompany", i) as DynamicObject;


                            if (kd["Code"].ToString()== "shunfengtongcheng") {
                                JObject postData = new JObject();
                                postData.Add("dev_id", appid);//同城开发者ID
                                postData.Add("push_time", timestamp);//推单时间
                                var value = this.Model.GetValue("FSourceOrderNO", i);
                                if (value == null)
                                {
                                    throw new Exception("第" + i + 1 + "单据编号为空");
                                }
                                postData.Add("user_order_id", this.Model.GetValue("FSourceOrderNO", i).ToString());//商家订单号

                                Log.Write("user_order_id:" + this.Model.GetValue("FSourceOrderNO", i).ToString());


                                postData.Add("company_id", business_id);//企业ID



                                postData.Add("settlement_type", 3);//结算方式，1同城月结 3大网月结 4余额支付
                                postData.Add("city_name", "杭州");//发单城市

                                var sendinfo=this.Model.GetValue("FSenderInfoList", i);
                                if (sendinfo == null)
                                {
                                    throw new Exception("第" + i + 1 + "寄件信息列表为空");
                                }

                                

                                string[] arr = sendinfo.ToString().Split('_');
                                string result = arr[arr.Length - 1];
                                Log.Write("sendinfo:" + result);

                                var sendPhone = this.Model.GetValue("FSenderPhone", i);
                                if (sendPhone == null)
                                {
                                    throw new Exception("第" + i + 1 + "寄件人手机号为空");
                                }
                                postData.Add("account_phone", Convert.ToInt64(sendPhone));//下单人手机号
                                Log.Write("sendPhone:" + sendPhone);

                                postData.Add("customer_code", result);//大网月结卡号

                                

                                JObject sender = new JObject();//发件人信息
                                sender.Add("sender_name", this.Model.GetValue("FSender", i).ToString());//发件人姓名
                                sender.Add("sender_phone", Convert.ToInt64(sendPhone));//发件人电话
                                var sendAddress = this.Model.GetValue("FFromAddress", i);//发件人地址
                                Log.Write("sender_name:" + this.Model.GetValue("FSender", i).ToString());
                                Log.Write("sender_phone:" + Convert.ToInt64(sendPhone));


                                if (sendAddress == null)
                                {
                                    throw new Exception("第" + i + 1 + "发件人地址为空");
                                }
                                sender.Add("sender_address", sendAddress.ToString());//发件人地址
                                Log.Write("sender_address:" + sendAddress.ToString());


                                String sendAddressresult = SendGetRequest(gaodeUrl + sendAddress.ToString());
                                JObject sendjson = JObject.Parse(sendAddressresult);
                                if ((string)sendjson["status"] == "1")
                                {
                                    string location = sendjson["geocodes"][0]["location"].ToString();
                                    string[] parts = location.Split(',');
                                    string jd = parts[0];
                                    string wd = parts[1];
                                    sender.Add("sender_lng", double.Parse(jd));//发件人经度
                                    sender.Add("sender_lat", double.Parse(wd));//发件人纬度
                                }
                                else
                                {
                                    throw new Exception("高德接口异常：" + sendjson["info"].ToString());
                                }
                                
                                


                                JObject receive = new JObject();//收件人信息
                                if (this.Model.GetValue("FReceiver", i) == null)
                                {
                                    throw new Exception("第" + i + 1 + "收件人姓名为空");
                                }
                                receive.Add("receiver_name", this.Model.GetValue("FReceiver", i).ToString());//收件人姓名
                                if (this.Model.GetValue("FReceiverPhone", i) == null)
                                {
                                    throw new Exception("第" + i + 1 + "收件人手机号为空");
                                }
                                receive.Add("receiver_phone", Convert.ToInt64(this.Model.GetValue("FReceiverPhone", i)));//收件人手机


                                var address=this.Model.GetValue("FToAddress", i);
                                if (address == null)
                                {
                                    throw new Exception("第" + i + 1 + "收件人地址为空");
                                }
                                receive.Add("receiver_address", address.ToString());//收件人地址

                                String gaoderesult = SendGetRequest(gaodeUrl+address.ToString());
                                JObject gaodejson = JObject.Parse(gaoderesult);
                                if ((string)gaodejson["status"] == "1")
                                {
                                    string location = gaodejson["geocodes"][0]["location"].ToString();
                                    string[] parts = location.Split(',');
                                    string jd = parts[0];
                                    string wd = parts[1];
                                    receive.Add("receiver_lng", double.Parse(jd));//收件人经度
                                    receive.Add("receiver_lat", double.Parse(wd));//纬度
                                }
                                else {
                                    throw new Exception("高德接口异常："+ gaodejson["info"].ToString());
                                }

                                    

                                JObject order_detail = new JObject();//物品信息
                                order_detail.Add("total_price", 500); //金额    100 表示1元
                                order_detail.Add("product_type", 16); //类型

                                postData.Add("sender", sender);
                                postData.Add("receiver", receive);
                                postData.Add("order_detail", order_detail);


                                String sign = generateOpenSign(postData.ToString(), appid, appKey);

                                Log.Write("url+sign：" + url + sign + "----postData:" + postData.ToString());


                                string backResult = Post(url + sign, postData.ToString());
                                Log.Write("物流返回结果：" + backResult);

                                //成功设置快递单号
                                JObject jsonObject = JObject.Parse(backResult);
                                if ((int)jsonObject["error_code"] == 0)
                                {
                                    string sfno = jsonObject["result"]["sf_order_id"].ToString();
                                    this.Model.SetValue("FKuaidiNum", sfno, i);
                                    
                                    Log.Write("顺丰单号：" + sfno);

                                    var kuaidibill = FormMetaDataCache.GetCachedFormMetaData(this.Context, "SAL_KuaidiBill");
                                    DynamicObject dynObj = new DynamicObject(kuaidibill.BusinessInfo.GetDynamicObjectType());

                                    //dynObj["FKuaidiCompany"] = kd;//快递公司


                                    dynObj["FKuaidiNum"] = sfno;
                                    dynObj["FReceiver"] = this.Model.GetValue("FReceiver", i).ToString();//收件人
                                    dynObj["FReceiverPhone"] = Convert.ToInt64(this.Model.GetValue("FReceiverPhone", i));//收件人手机
                                    dynObj["FToAddress"] = address.ToString();//收件人地址
                                    dynObj["FSender"] = this.Model.GetValue("FSender", i).ToString();//寄件人
                                    dynObj["FSenderPhone"] = Convert.ToInt64(sendPhone);//寄件人手机
                                    dynObj["FFromAddress"] = this.Model.GetValue("FFromAddress", i);//寄件人地址
                                    dynObj["FCount"] = 1;//商品数量
                                    dynObj["FSourceOrderNO"] = this.Model.GetValue("FSourceOrderNO", i).ToString();//关联单据编号
                                    dynObj["FTimestamp"] = Convert.ToDateTime(TimeServiceHelper.GetSystemDateTime(this.Context).ToString("yyyy-MM-dd HH:mm:ss"));
                                    dynObj["FCreateDate"] = Convert.ToDateTime(TimeServiceHelper.GetSystemDateTime(this.Context).ToString("yyyy-MM-dd HH:mm:ss"));//关联单据编号
                                    // 提交数据库保存，并获取保存结果

                                    var saveResult = BusinessDataServiceHelper.Save(this.Context, kuaidibill.BusinessInfo, new DynamicObject[] { dynObj });
                                    if (saveResult.IsSuccess)
                                    {
                                        string strsql = string.Format(@"
                                            /*dialect*/ 
                                            IF NOT EXISTS(SELECT 1 FROM T_SAL_DELIVERYNOTICETRACE WHERE FID = {0} and FCARRYBILLNO = '{1}')
                                            BEGIN
                                                INSERT INTO T_SAL_DELIVERYNOTICETRACE(FID, FENTRYID, FLOGCOMID, FCARRYBILLNO, FPHONENUMBER,FCARRYBILLNOTYPE)
                                                VALUES({0}, (select max(FENTRYID) + 1 from T_SAL_DELIVERYNOTICETRACE), (select FID from T_BD_KD100LOGISTICSCOM where FNAME='顺丰同城急送'),'{1}', {2},'M');
                                            END
                                            ELSE
                                            BEGIN
                                                UPDATE T_SAL_DELIVERYNOTICETRACE SET FLOGCOMID = (select FID from T_BD_KD100LOGISTICSCOM where FNAME='顺丰同城急送'), FPHONENUMBER = {2} WHERE FID = {0} AND FCARRYBILLNO = '{1}';
                                            END; 
                                            ", this.Model.GetValue("FSourceOrderId", i), sfno, sendPhone);//单据id，物流单号，寄件人手机号，物流公司

                                        DBServiceHelper.Execute(this.Context, strsql);
                                    }
                                    else
                                    {
                                        List<ValidationErrorInfo> error = saveResult.ValidationErrors;
                                        Log.Write("保存电子面单列表失败:" + error.ToString());
                                    }

                                }

                                Log.Write("物流返回结果：" + jsonObject["error_msg"].ToString());
                            }
                        }
                        catch (Exception ex){
                            
                            Log.Write("抛出的错误信息：" + ex.Message);
                        }
                    }

                    break;
            }
        }



        public static String generateOpenSign(String postData, long appId, String appKey)
        {
            /*StringBuilder sb = new StringBuilder();
            sb.Append(postData);
            sb.Append("&" + appId + "&" + appKey);
            Log.Write("需要md5加密数据：" + sb.ToString());*/
            string data=string.Format(@"{0}&{1}&{2}", postData, appId, appKey);
            Log.Write($"待加密数据："+ data);
            string ret= DoMD5v2(data.ToString());
            return ret;
        }

        static string SendGetRequest(string url)
        {
            try
            {
                // 创建一个 HTTP 请求
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

                // 设置请求方法为 GET
                request.Method = "GET";

                // 获取响应
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    // 读取响应流
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write($"高德接口异常：{ex.Message}, 时间：{DateTime.Now.ToString()}");
                Console.WriteLine("请求发生异常: " + ex.Message);
                return string.Empty;
            }
        }

        public static string Post(string Url, string jsonParas)
        {
            string strURL = Url;
            // 创建一个 HTTP 请求
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(strURL);
            // 设置请求方法为 POST
            request.Method = "POST";
            // 设置内容类型为 application/json
            request.ContentType = "application/json";

            // 设置请求头的 Auth 字段request.Headers.Add("Auth", auth);


            // 将请求参数转换为字节数组
            byte[] payload = Encoding.UTF8.GetBytes(jsonParas);

            // 设置请求的 ContentLength
            request.ContentLength = payload.Length;

            // 发送请求，获取请求流
            Stream writer;
            try
            {
                writer = request.GetRequestStream();
            }
            catch (Exception)
            {
                writer = null;
                Console.Write("连接服务器失败!");
            }

            // 将请求参数写入流
            writer.Write(payload, 0, payload.Length);
            writer.Close(); // 关闭请求流

            HttpWebResponse response;
            try
            {
                // 获取响应流
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException ex)
            {
                response = ex.Response as HttpWebResponse;
            }

            Stream s = response.GetResponseStream();
            StreamReader sRead = new StreamReader(s);
            string postContent = sRead.ReadToEnd();
            sRead.Close();
            Log.Write($"服务端返回信息：{postContent}, 时间：{DateTime.Now.ToString()}");
        
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"服务端返回信息：{postContent}, 时间：{DateTime.Now.ToString()}");

            return postContent; // 返回 JSON 数据
        }

        public static string DoMD5(string prestr)
        {
            StringBuilder sb = new StringBuilder(32);
            byte[] t = md5.ComputeHash(Encoding.GetEncoding("utf-8").GetBytes(prestr));
            for (int i = 0; i < t.Length; i++)
            {
                sb.Append(t[i].ToString("x").PadLeft(2, '0'));
            }
            return sb.ToString();
        }
        public static string DoMD5v2(string prestr)
        {
            
            byte[] md5Bytes = md5.ComputeHash(Encoding.GetEncoding("utf-8").GetBytes(prestr));
            StringBuilder sbHex = new StringBuilder();
            foreach (byte b in md5Bytes)
            {
                sbHex.Append(b.ToString("x2"));
            }

            byte[] utf8Bytes = Encoding.UTF8.GetBytes(sbHex.ToString());
            string base64Encoded = Convert.ToBase64String(utf8Bytes);

            return base64Encoded;
        }
    



        static string CalculateMD5(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    builder.Append(hashBytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

    }
}
/*
for (int i = 0; i < entity; i++)
                    {
                        DynamicObject kd = this.Model.GetValue("FKuaidiCompany", i) as DynamicObject;
                        //string ceshi=kd["Name"].ToString();

                        JObject postData = new JObject();
                        postData.Add("dev_id", appid);//同城开发者ID
                        postData.Add("push_time", timestamp);//推单时间
                        postData.Add("user_order_id", "12785112q");//商家订单号

                        postData.Add("company_id", "1161885087");//企业ID
                        //postData.Add("shop_id", 3243279847393);//店铺ID


                        postData.Add("settlement_type", 3);//结算方式，1同城月结 3大网月结 4余额支付
                        postData.Add("city_name", "北京");//发单城市


                        postData.Add("account_phone", 13203559287);//下单人手机号
                        postData.Add("customer_code", 5717110613);//大网月结卡号


                        JObject sender = new JObject();//发件人信息
                        sender.Add("sender_name", "顺丰同城");//发件人姓名
                        sender.Add("sender_phone", "13881979410");//发件人电话
                        sender.Add("sender_address", "北京市海淀区学清嘉创大厦A座15层");//发件人地址
                        sender.Add("sender_lng", 116.377333);//发件人经度
                        sender.Add("sender_lat", 40.032412);//发件人纬度


                        JObject receive = new JObject();//收件人信息
                        receive.Add("receiver_name", "顺丰同城");//收件人姓名
                        receive.Add("receiver_phone", "18968188497");//收件人手机
                        receive.Add("receiver_address", "北京市海淀区学清嘉创大厦A座15层");//收件人地址
                        String gaoderesult = SendGetRequest(gaodeUrl+"北京市海淀区学清嘉创大厦A座15层");
                        JObject gaodejson = JObject.Parse(gaoderesult);
                        if ((string)gaodejson["status"] == "1")
                        {
                            string location = gaodejson["geocodes"][0]["location"].ToString();
                            string[] parts = location.Split(',');
                            string jd = parts[0];
                            string wd = parts[1];
                            receive.Add("receiver_lng", double.Parse(jd));//收件人经度
                            receive.Add("receiver_lat", double.Parse(wd));//纬度
                        }
                        else
                        {
                            throw new Exception("高德接口异常：" + gaodejson["info"].ToString());
                        }

                        JObject order_detail = new JObject();//物品信息
                        order_detail.Add("total_price", 500); //金额    100 表示1元
                        order_detail.Add("product_type", 16); //类型

                        postData.Add("sender", sender);
                        postData.Add("receiver", receive);
                        postData.Add("order_detail", order_detail);


                        String sign = generateOpenSign(postData.ToString(), appid, appKey);

                        Log.Write("url+sign：" + url + sign + "----postData:" + postData.ToString());


                        string backResult = Post(url + sign, postData.ToString());


                        JObject jsonObject = JObject.Parse(backResult);
                        if ((int)jsonObject["error_code"] == 0)
                        {
                            string sfno= jsonObject["result"]["sf_order_id"].ToString();
                            this.Model.SetValue("FKuaidiNum", sfno, i);
                            Log.Write("顺丰单号：" + sfno);
                        }

                            Log.Write("物流返回结果：" + backResult);
                    }*/
