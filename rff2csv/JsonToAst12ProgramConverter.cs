using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace rff2csv
{
    internal class JsonToAst12ProgramConverter
    {
        private readonly string JsonData;

        public JsonToAst12ProgramConverter(string jsonString)
        {
            JsonData = jsonString;
        }

        public IEnumerable<ProcessDataSet> GetDataSet()
        {
            JArray val = JArray.Parse("[" + JsonData + "]");
            List<ProcessDataSet> list = new List<ProcessDataSet>();
            foreach (JToken item in val)
            {
                JObject val2 = (JObject)(object)((item is JObject) ? item : null);
                if (val2 == null)
                {
                    continue;
                }

                foreach (KeyValuePair<string, JToken> item2 in val2)
                {
                    ProcessDataSet dataSet = GetDataSet(item2.Key, item2.Value);
                    if (dataSet == null)
                    {
                        continue;
                    }

                    if (dataSet.TextIdentifier.IsTheSameIgnoreCase("@TEXT_Steps"))
                    {
                        List<ProcessDataSet> list2 = (List<ProcessDataSet>)dataSet.Value;
                        if (list2 != null)
                        {
                            for (int i = 0; i < list2.Count; i++)
                            {
                                list2[i].TextIdentifier = i + 1 + ". " + list2[i].TextIdentifier;
                            }
                        }
                    }

                    list.Add(dataSet);
                }
            }

            return list;
        }

        private static ProcessDataSet GetDataSet(string key, JToken content)
        {
            //IL_001d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0022: Unknown result type (might be due to invalid IL or missing references)
            //IL_009a: Unknown result type (might be due to invalid IL or missing references)
            //IL_00cc: Unknown result type (might be due to invalid IL or missing references)
            //IL_00f9: Unknown result type (might be due to invalid IL or missing references)
            //IL_00fe: Unknown result type (might be due to invalid IL or missing references)
            //IL_010e: Unknown result type (might be due to invalid IL or missing references)
            ProcessDataSet processDataSet = new ProcessDataSet();
            if (((IEnumerable<JToken>)content).Count() > 1)
            {
                MakeTextVisible(key, processDataSet);
                List<ProcessDataSet> list = new List<ProcessDataSet>();
                foreach (JToken item in content.Children())
                {
                    list.Add(GetDataSet("", item));
                }

                processDataSet.Value = list;
            }
            else
            {
                JProperty val = (JProperty)(object)((content is JProperty) ? content : null);
                if (val != null)
                {
                    HandleJProperty(val.Name, val, processDataSet);
                }
                else
                {
                    JValue val2 = (JValue)(object)((content is JValue) ? content : null);
                    if (val2 != null)
                    {
                        MakeTextVisible(((JProperty)((JToken)val2).Parent).Name, processDataSet);
                        processDataSet.Value = val2.Value.ToString();
                    }
                    else if (content != null)
                    {
                        JToken obj = ((IEnumerable<JToken>)(object)content.Children()).First();
                        JProperty val3 = (JProperty)(object)((obj is JProperty) ? obj : null);
                        if (val3 != null)
                        {
                            HandleJProperty(val3.Name, val3, processDataSet);
                        }
                        else
                        {
                            JEnumerable<JToken> val4 = content.Children();
                            List<ProcessDataSet> list2 = new List<ProcessDataSet>();
                            MakeTextVisible(key, processDataSet);
                            foreach (JToken item2 in (IEnumerable<JToken>)Extensions.Children<JToken>((IEnumerable<JToken>)(object)val4))
                            {
                                list2.Add(GetDataSet(key, item2));
                            }

                            processDataSet.Value = list2;
                        }
                    }
                }
            }

            return processDataSet;
        }

        private static void HandleJProperty(string key, JProperty prop, ProcessDataSet ds)
        {
            MakeTextVisible(key, ds);
            string propValue = (string)(ds.Value = ((object)prop.Value).ToString().Trim());
            if (IsJson(propValue))
            {
                ConvertJsonToProcessDataSetList(prop, ds);
            }
        }

        private static void MakeTextVisible(string value, ProcessDataSet ds)
        {
            ds.TextIdentifier = "@TEXT_" + value;
        }

        private static void ConvertJsonToProcessDataSetList(JProperty jProperty, ProcessDataSet ds)
        {
            try
            {
                List<ProcessDataSet> list = new List<ProcessDataSet>();
                Dictionary<string, object> dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(((object)jProperty.Value).ToString());
                foreach (KeyValuePair<string, object> item in dictionary)
                {
                    list.Add(new ProcessDataSet
                    {
                        TextIdentifier = "@TEXT_" + item.Key,
                        Value = item.Value
                    });
                }

                ds.Value = list;
            }
            catch (JsonSerializationException)
            {
            }
        }

        private static bool IsJson(string propValue)
        {
            if (propValue.StartsWith("{", StringComparison.InvariantCultureIgnoreCase) && propValue.Contains("\":"))
            {
                return propValue.EndsWith("}", StringComparison.InvariantCultureIgnoreCase);
            }

            return false;
        }
    }
}