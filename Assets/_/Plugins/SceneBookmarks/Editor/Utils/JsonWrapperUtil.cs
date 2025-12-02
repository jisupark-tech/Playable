using System;
using UnityEditor;

namespace SceneBookmarks
{
    public class JsonWrapperUtil<T>
    {
        public T[] data;

        public static string ToJson(T[] data)
        {
            JsonWrapperUtil<T> obj = new JsonWrapperUtil<T>() { data = data };
            return EditorJsonUtility.ToJson(obj);
        }

        public static T[] FromJson(string json)
        {
            JsonWrapperUtil<T> obj = new JsonWrapperUtil<T>();
            EditorJsonUtility.FromJsonOverwrite(json, obj);
            if (obj == null)
            {
                return null;
            }
            else
            {
                return obj.data;
            }
        }
    }

}
