using System;

public class PlanerItem
{
    public DateTime Data { get; set; } 
    public string Tytul { get; set; }  
    public string Opis { get; set; }   
    public bool CzyZrealizowane { get; set; } 
    public string FormatowanaData => Data.ToShortDateString(); 
}