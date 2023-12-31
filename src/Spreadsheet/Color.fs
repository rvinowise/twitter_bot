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

type Google_color = Google.Apis.Sheets.v4.Data.Color

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
        
    let from_google_color (google_color:Google_color) =
        {
            r=float google_color.Red.Value
            g=float google_color.Green.Value
            b=float google_color.Blue.Value
        }

    
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
    
    let darkness_strength_from_mixing_colors
        colors_amount
        total_color_strength
        =
        let max_possible_darkness = colors_amount-1.0
            
        let darkness_coefficient =
            max 0.0 (total_color_strength-1.0)
        
        darkness_coefficient / max_possible_darkness
    
    let divide divisor (color:Color) =
        {
            r=color.r/divisor
            g=color.g/divisor
            b=color.b/divisor
        }
    
    let surplus (color:Color) =
         {
             r=
                 (1.0 - color.r)
                 |>min 0 |>abs
             g=
                 (1.0 - color.g)
                |>min 0 |>abs
             b=
                (1.0 - color.b)
                |>min 0 |>abs
        }
    let has_surplus (color:Color) =
        color.r > 1 || color.g > 1 || color.b > 1
    let normalize (color:Color) =
        if has_surplus color then
            let biggest_tint =
                List.max [color.r;color.g;color.b]
            {
                r=color.r/biggest_tint
                g=color.g/biggest_tint
                b=color.b/biggest_tint
            }
        else color
    
    let remove_color base_color removed_color =
        {
            r=base_color.r - removed_color.r
            g=base_color.g - removed_color.g
            b=base_color.b - removed_color.b
        }
    
    let darkness_from_surplus (surplus:Color) =
        surplus.r+surplus.g+surplus.b
            
    let mix_colors
        (base_color: Color)
        (colors: (Color*float) list)
        =
        let colors_amount=
            colors|>List.length|>float
            
        let total_strength =
            colors
            |>List.map snd
            |>List.reduce (+)

        let darkness_strength =
            darkness_strength_from_mixing_colors
                colors_amount
                total_strength
        
        colors
        |>List.fold(fun base_color (added_color, added_amount) ->
            if total_strength > 1 then
                base_color
                |>mix_two_colors
                    added_color
                    (added_amount/(total_strength))
            else
                base_color
                |>mix_two_colors
                    added_color
                    added_amount
        )
            white
        |>mix_two_colors
            black darkness_strength