using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data.SqlClient;
using Dapper;
using System.Collections;
using System.Xml;
using System.Data;
using SqlKata;
using SqlKata.Compilers;


namespace ExportMinRemains
{
    class Program
    {
        /// <summary>
        /// Получаем таблицу по запросу или имени 
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public static DataTable fillDataTable(string table, string connectionString, string Databse="eplus_work")
        {
            string query = table;

            //костыль - так лучше не делать
            if (table.ToUpper().Contains("SELECT") == true)
            {
                query = table;
            }
            else
            {
                query = "SELECT * FROM " + Databse + ".dbo." + table;
            }



            String conSTR = connectionString;
            SqlConnection sqlConn = new SqlConnection(conSTR);

            sqlConn.Open();
            SqlCommand cmd = new SqlCommand(query, sqlConn);
            cmd.CommandTimeout = 0;
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            da.Fill(dt);
            sqlConn.Close();
            return dt;
        }

        /// <summary>
        /// Обновление config  файла ПО из комплекта  F3tail
        /// </summary>
        /// <param name="appConfigPath"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        static void UpdateConfigFile(string appConfigPath, string key, string value)
        {
            var appConfigContent = File.ReadAllText(appConfigPath);
            var searchedString = $"<add key=\"{key}\" value=\"";
            var index = appConfigContent.IndexOf(searchedString) + searchedString.Length;
            var currentValue = appConfigContent.Substring(index, appConfigContent.IndexOf("\"", index) - index);
            var newContent = appConfigContent.Replace($"{searchedString}{currentValue}\"", $"{searchedString}{value}\"");
            File.WriteAllText(appConfigPath, newContent);
        }


        /// <summary>
        /// Получаем из файла с параметрами ефармы строку подключения
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static Hashtable getSettings(string path)
        {
            Hashtable _ret = new Hashtable();
            if (File.Exists(path))
            {
                StreamReader reader = new StreamReader
                (
                    new FileStream(
                        path,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read)
                );
                XmlDocument doc = new XmlDocument();
                string xmlIn = reader.ReadToEnd();
                reader.Close();
                doc.LoadXml(xmlIn);
                XmlNodeList xnList = doc.DocumentElement.SelectNodes("appSettings");
                foreach (XmlNode node in xnList)
                    foreach (XmlNode addnode in node.ChildNodes)
                        if (addnode.Name.Equals("add"))
                            _ret.Add
                            (
                                addnode.Attributes["key"].Value,
                                addnode.Attributes["value"].Value
                            );

            }
            return (_ret);
        }

        /// <summary>
        /// Ищем строку подключения в настройках  F3Tail чтобы не писать руками
        /// </summary>
        /// <returns></returns>
        static string GetConnection()
        {
            //строка подключения из файла настроек самой выгрузки
            String ConnectionString = Properties.Settings.Default.ConnectionString;

            if (ConnectionString!="")
            {
                Console.WriteLine("Строка подключения есть в файле конфигурации");
                return ConnectionString;
            }

            String conf = "";

            if (File.Exists(Path.Combine(@"c:\efarma2\client", "ePlus.Client.exe.Config")))
                conf = Path.Combine(@"c:\efarma2\client", "ePlus.Client.exe.Config");

            if (File.Exists(Path.Combine(@"c:\f3tail\client", "ePlus.Client.exe.Config")))
                conf = Path.Combine(@"c:\f3tail\client", "ePlus.Client.exe.Config");

            if (File.Exists(Path.Combine(@"L:\OFFICE-RIGLA\eFarma2\Client", "ePlus.Client.exe.Config")))
                conf = Path.Combine(@"L:\OFFICE-RIGLA\eFarma2\Client", "ePlus.Client.exe.Config");

            if (File.Exists(Path.Combine(@"d:\f3tail\client", "ePlus.Client.exe.Config")))
                conf = Path.Combine(@"d:\f3tail\client", "ePlus.Client.exe.Config");


            // Или можно руками сокпировать файл конфигурации и положить рядом с 
            if (File.Exists(Path.Combine(Environment.CurrentDirectory, "ePlus.Client.exe.Config")))
                conf = Path.Combine(Environment.CurrentDirectory, "ePlus.Client.exe.Config");



            if (conf == "")
            {
                throw new Exception("Не найден файл с конфигурацией - делаем все ручками");
            }
            else
            {
                Console.WriteLine("Найден файл конфигурации");
                Console.WriteLine(conf);
            }
            

            
            if (File.Exists(conf))
            {
                Hashtable ht = new Hashtable();
                try
                {
                    ht = getSettings(conf);
                }
                finally
                {
                    ConnectionString = ht["ConnectionString"].ToString();
                }

            }
            
            return ConnectionString;
        }


        static void Main(string[] args)
        {

            string connection = GetConnection();
            List<string> otdelList = new List<string>();
            using (SqlConnection cnn= new SqlConnection(connection))
            {
                string comm = "select name  from STORE where ID_CONTRACTOR in (select ID_CONTRACTOR from   DBO.FN_SELF_CONTRACTORS())";// File.ReadAllText(tableName); 
                otdelList = cnn.Query<string>(comm).ToList();
            }

            Console.WriteLine("--- " + "Список складов"+ " ---");
            foreach (string s in otdelList)
                Console.WriteLine(s);

            Console.WriteLine();
            Console.WriteLine("Введите часть названия склада");
            Console.WriteLine();
            String userInput = Console.ReadLine();

            if (userInput== "")
            {
                /* 
                 Console.WriteLine("Выходим. Ничего не делаем");
                 return; 
                */
                userInput = "Мин";
            }

            string sklad = "";
            foreach (string s in otdelList)
            {
                if (s.ToUpper().Contains(userInput.ToUpper()))
                {
                    sklad = s;
                    break;
                }
            }

            if (sklad != "")
            {
                Console.WriteLine("Выбран " + sklad);
            }
            else
            {
                Console.WriteLine("Склад не выбран");
                return;
            }


            string SQlStr = File.ReadAllText("Remains.sql");
            DataTable dt;
            using (SqlConnection cnn = new SqlConnection(connection))
            {

                SQlStr += " AND dbo.STORE.NAME='" + sklad + "'";
                dt = fillDataTable(SQlStr, connection);
                Console.WriteLine("Получено ",  dt.Columns.Count, " строк");
                
            }

            //все поставщики
            //List<string> contractors = new List<string>();
            //contractors = dt.Select("CONTRACOR_NAME").ToList()

            //Уникальные товары
            //строим файл с группировкой по товарам
            List<string> contractors = dt.AsEnumerable()
                 .Select(s => s.Field<string>("CONTRACTOR_NAME"))
                 .Distinct()
                 .ToList();
            foreach (string contractor in contractors)
            {
                Console.WriteLine();
                Console.WriteLine("Поставщик "+ contractor);

                using (SqlConnection cnn = new SqlConnection(connection))
                {

                    
                    
                    dt = fillDataTable(SQlStr + " AND CONTRACTOR.NAME= '" + contractor +"'" , connection);
                    List<string> documents = dt.AsEnumerable()
                        .Select(s => s.Field<string>("INVOICE"))
                        .Distinct()
                        .ToList();

                    foreach (string document in documents)
                    {
                        ExportDocument(document, dt, contractor);
                    }


                }

               
            }
            Console.WriteLine();
            Console.WriteLine("Все документы сформированы. Нажмите любую кнопочку.");
            Console.ReadKey();


        }

        public static void ExportDocument(string document, DataTable dt, string contractor)
        {
            
            
            //fName = DateTime.Now.ToString("yyyy-MM-dd ") + "~" + Properties.Settings.Default.SubID + fName;
            // fName = DateTime.Now.ToString() + "~" + Properties.Settings.Default.SubID + fName;
            // fName = DateTime.Now.ToString() + "~" + fName;
            string fName = contractor + document+ ".csv";// Properties.Settings.Default.ID + "_" + Properties.Settings.Default.SubID + "_" + DateTime.Now.ToString("yyyyMMdd") + "T" + DateTime.Now.ToString("HHmm") + fName;
            String pPath = Directory.GetCurrentDirectory();
            /*
            fName = fName.Replace(" ", "-");
            fName = fName.Replace(":", "-");
            fName = fName.Replace(@"\"," ");
            fName = fName.Replace("/", " ");
            */
            fName = Path.GetInvalidFileNameChars().Aggregate(fName, (current, invalid_char) => current.Replace(invalid_char.ToString(), "_"));
            Console.WriteLine("Writing " + fName);
           // using (var output = new StreamWriter(Path.Combine(pPath, fName), false, Encoding.GetEncoding("Windows-1251"))) // добавить дату fname
            //{
                #region
                /*
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandTimeout = 0;
                    cmd.CommandText = tableName;// File.ReadAllText(tableName);
                    try
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            WriteHeader(reader, output);
                            while (reader.Read())
                            {
                                WriteData(reader, output);

                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(DateTime.Now.ToString());
                        Console.Write(ex.ToString());

                    }
                }
                */
                #endregion

            StringBuilder sb = new StringBuilder();
            string docdate ="";
            string docnumber = "";
            //   
            decimal summa = 0;
            foreach (DataRow row in dt.AsEnumerable())
                {
                    if (row["INVOICE"].ToString() != document)
                        continue;
                    summa += decimal.Parse(row["PRICE_SUP"].ToString()) * decimal.Parse(row["QUANTITY_REM"].ToString());
                   docdate = row["INVOICE_DATE"].ToString();
                   docnumber = row["INVOICE"].ToString();
               //     sb.AppendLine("[BODY]");
                    
                foreach (DataColumn dataColumn in dt.Columns)
                    {
                        string fieldValue = row[dataColumn].ToString();
                        
                        //    output.Write(';');
                        String v = row[dataColumn].ToString();
                        if (row[dataColumn].GetType().FullName == "System.Decimal")
                            v = v.Replace(",", ".");

                         if (row[dataColumn].GetType().FullName == "System.Boolean")
                         {
                            v=v.Replace("True", "1");
                            v=v.Replace("False","0");
                         }
                        if (v.Contains(';') || v.Contains('\n') || v.Contains('\r') || v.Contains('"'))
                        {
                            //output.Write('"');
                            v=v.Replace("\"", "\"\"");
                            //output.Write('"');
                        }
                        else
                        {

                            sb.Append(v);
                        }

                        sb.Append(";"); 

                    }
                    sb.AppendLine();

                }

            StringBuilder sb2 = new StringBuilder();
            sb2.AppendLine("[Header]");
            sb2.Append(docdate); //188820592 - 002; 14.05.2022; 1388.74; ПОСТАВКА; 0.00; 231.46; РУБЛЬ; 69.42; 0.00; 08987 / 20 от 28.12.2020; ЦВ Протек; 285947; Здоровье г.Петропавловск - Камчатский; 300005; Здоровье(ул.Корфская, 6) г.Петр.- Камч.; 00000000173615; 00000000107143;
            sb2.Append(";");
            sb2.Append(docnumber);
            sb2.Append(";");
            sb2.Append(summa.ToString().Replace(",", "."));
            sb2.AppendLine();
            sb2.AppendLine("[Body]");
            sb2.Append(sb.ToString());
            File.WriteAllText(fName ,sb2.ToString(), Encoding.GetEncoding(1251));
            

        }



        /// <summary>
        /// Эксопрт документа в файл 
        /// </summary>
        /// <param name="document"></param>
        /// <param name="dt"></param>
        private static void ExportDocument(string document, DataTable dt)
        {
            throw new NotImplementedException();
        }
    }
}
