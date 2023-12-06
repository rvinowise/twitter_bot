namespace rvinowise.twitter

open System
open System.Collections.Generic
open Google.Apis.Sheets.v4.Data
open Google.Apis.Sheets.v4
open Xunit


type Color = {
    red:float
    green:float
    blue:float
    alpha:float
}

module Color =
    let to_googlesheet_color (color:Color) =
        
        Data.Color(
            Red= float32 color.red,
            Green= float32 color.green,
            Blue= float32 color.blue,
            Alpha= float32 color.alpha
        )
    
    let to_google_color (color:Color) =
        Color(
            Blue = float32 color.blue,
            Red = float32 color.red,
            Green = float32 color.green,
            Alpha = float32 color.alpha
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
            red=color_from.red * multiplier_from + color_to.red*multiplier_to
            green=color_from.green * multiplier_from + color_to.green*multiplier_to
            blue=color_from.blue * multiplier_from + color_to.blue*multiplier_to
            alpha=color_from.alpha * multiplier_from + color_to.alpha*multiplier_to
        }
        
       
        
    