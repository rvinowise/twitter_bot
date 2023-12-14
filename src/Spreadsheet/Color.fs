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
        
    let coefficient_between_values
        (value_from: int)
        (value_to: int)
        (value_between: int)
        =
        float(value_between-value_from)
        /
        float(value_to-value_from)

    
    let cell_color_for_value
        (color_from:Color)
        (color_to:Color)
        (value_from: int)
        (value_to: int)
        (value_between: int)
        =
        let multiplier_to =
            coefficient_between_values
                value_from
                value_to
                value_between
        let multiplier_from = 1.0-multiplier_to
        
        {
            r=color_from.r * multiplier_from + color_to.r*multiplier_to
            g=color_from.g * multiplier_from + color_to.g*multiplier_to
            b=color_from.b * multiplier_from + color_to.b*multiplier_to
        }
        
       
        
    