using UnityEngine;

public static class GameDataBaseLoader
{
    private static GameDataBase instance;
    public static GameDataBase Instance
    {
        get{

            if (instance == null) 
            {
                instance = Resources.Load<GameDataBase>("GameDataBase");
            }
            return instance;
        } 
    }
}
