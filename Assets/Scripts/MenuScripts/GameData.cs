using System;
using System.Collections.Generic;

[Serializable]
public class GameData
{
    public string version = "1.0";
    public string sceneName;
    public long savedUnixTime;

    // Map of SaveableEntity.UniqueId -> arbitrary component state (JSON-friendly)
    public Dictionary<string, object> entities = new Dictionary<string, object>();
}
