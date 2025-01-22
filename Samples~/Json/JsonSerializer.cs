using UnityEngine;
namespace TCS.Bootstrapper {
    public class JsonSerializer : ISerializer {
        public string Serialize<T>(T obj) {
            return JsonUtility.ToJson(obj, true);
        }

        public T Deserialize<T>(string json) {
            return JsonUtility.FromJson<T>(json);
        }
        
        public T DeserializeFromTextAsset<T>(TextAsset json) {
            return JsonUtility.FromJson<T>(json.text);
        }
    }
}