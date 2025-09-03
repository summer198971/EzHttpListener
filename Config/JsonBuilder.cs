using System;
using System.Collections.Generic;
using System.Text;

namespace EzHttpListener
{
    public class JsonBuilder
    {
        private readonly Dictionary<string, string> _elements;
        bool change = false;
        string _json;

        public JsonBuilder()
        {
            _elements = new Dictionary<string, string>();
            change = true;
            _json = "";
        }
        public JsonBuilder(string json)
        {
            Console.WriteLine("JsonBuilder: "+json);
            _json = json;
            _elements = new Dictionary<string, string>();
            ReadAllJson();
        }
        public void ReadAllJson()
        {
            _elements.Clear();
            if(string.IsNullOrEmpty(_json))
            {
                return;
            }
            change = true;
            try
            {
                var jsonDoc = System.Text.Json.JsonDocument.Parse(_json);
                var root = jsonDoc.RootElement;
                
                if (root.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    foreach (var property in root.EnumerateObject())
                    {
                        Console.WriteLine("JsonBuilder: "+property.Name+" "+property.Value.ToString());
                        _elements[property.Name] = property.Value.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析JSON数据失败: {ex.Message}");
            }
        }
        public string GetValue(string key)
        {
            if(_elements!=null)
            {
                if (_elements.ContainsKey(key))
                {
                    return _elements[key];
                }
            }
            return null;
        }
        public string GetValue(string key, string defaultValue)
        {
            if(_elements!=null)
            {
                if (_elements.ContainsKey(key))
                {
                    return _elements[key];
                }
            }
            return defaultValue;
        }
        public void Add(string key, string value)
        {
            _elements[key] = value;
            change = true;
        }

        public void Add(string key, int value)
        {
            _elements[key] = value.ToString();
            change = true;
        }

        public void Add(string key, bool value)
        {
            _elements[key] = value.ToString().ToLower();
            change = true;
        }

        public string ToLineString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var element in _elements)
            {
                sb.Append(element.Key).Append(":").Append(element.Value).Append("\n");
            }
            return sb.ToString();
        }

        public override string ToString()
        {
            if (!change)
            {
                return _json;
            }

            change = false;
            string json = null;
            try
            {
                json = System.Text.Json.JsonSerializer.Serialize(_elements);
            }
            catch (Exception e)
            {
                Console.WriteLine("JsonBuilder.ToString() error: " + e.Message);
            }

            if (!string.IsNullOrEmpty(json))
            {
                _json = json;
            }

            return _json;
        }

        public void Clear()
        {
            _elements.Clear();
        }
    }
}