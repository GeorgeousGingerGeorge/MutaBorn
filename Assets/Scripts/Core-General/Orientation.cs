using UnityEngine;

[System.Serializable]
public enum FacingDirection //Used for units 
{ 
    North = 0,
    NorthEast = 45,
    East = 90,
    SouthEast = 135,
    South = 180,
    SouthWest = 225,
    West = 270,
    NorthWest = 315
}

[System.Serializable]
public enum OpeningDirection //Determines the direction a door would swing

{
    None,
    Inward,
    Outward,
    Left,
    Right
}