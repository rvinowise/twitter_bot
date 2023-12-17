namespace rvinowise.twitter

open System
open System.Collections.Generic
open Google.Apis.Sheets.v4.Data
open Google.Apis.Sheets.v4
open Xunit


type Color = {
    r:float
    g:float
    b:float
}

module Color =
    
    let white = {r=1;g=1;b=1}
    let black = {r=0;g=0;b=0}
    let to_googlesheet_color (color:Color) =
        
        Data.Color(
            Red= float32 color.r,
            Green= float32 color.g,
            Blue= float32 color.b
        )
    
    let to_google_color (color:Color) =
        Color(
            Blue = float32 color.b,
            Red = float32 color.r,
            Green = float32 color.g
        )
        
    

    
    let mix_two_colors
        (added_color:Color)
        (amount: float)
        (base_color:Color)
        =
        let multiplier_from = 1.0-amount
        {
            r=base_color.r * multiplier_from + added_color.r*amount
            g=base_color.g * multiplier_from + added_color.g*amount
            b=base_color.b * multiplier_from + added_color.b*amount
        }
    
        
       
        
    