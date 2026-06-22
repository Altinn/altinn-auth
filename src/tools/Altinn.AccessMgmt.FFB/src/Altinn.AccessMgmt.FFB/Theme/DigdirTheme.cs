using MudBlazor;

namespace Altinn.AccessMgmt.FFB.Theme;

public static class DigdirTheme
{
    public static readonly MudTheme Instance = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#0062BA",
            PrimaryDarken = "#004F95",
            PrimaryLighten = "#3381C8",
            PrimaryContrastText = "#FFFFFF",

            Secondary = "#5B3FA0",
            SecondaryDarken = "#482F80",
            SecondaryLighten = "#7B62B5",
            SecondaryContrastText = "#FFFFFF",

            Tertiary = "#068718",
            TertiaryContrastText = "#FFFFFF",

            Success = "#068718",
            Warning = "#EA9B1B",
            Error = "#C01B1B",
            Info = "#0A71C0",

            Background = "#F4F5F6",
            Surface = "#FFFFFF",
            DrawerBackground = "#FFFFFF",
            AppbarBackground = "#0062BA",
            AppbarText = "#FFFFFF",

            TextPrimary = "#24272B",
            TextSecondary = "#4A4F55",
            TextDisabled = "rgba(36,39,43,0.38)",
            Divider = "rgba(36,39,43,0.12)",
            ActionDefault = "#24272B",
        },

        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Inter", "system-ui", "sans-serif"],
                FontSize = "1rem",
                LineHeight = "1.5",
                FontWeight = "400",
            },
            H1 = new H1Typography { FontSize = "2.5rem", FontWeight = "600", LineHeight = "1.3" },
            H2 = new H2Typography { FontSize = "2rem", FontWeight = "600", LineHeight = "1.3" },
            H3 = new H3Typography { FontSize = "1.75rem", FontWeight = "500", LineHeight = "1.3" },
            H4 = new H4Typography { FontSize = "1.5rem", FontWeight = "500", LineHeight = "1.3" },
            H5 = new H5Typography { FontSize = "1.25rem", FontWeight = "500", LineHeight = "1.3" },
            H6 = new H6Typography { FontSize = "1rem", FontWeight = "500", LineHeight = "1.3" },
            Body1 = new Body1Typography { FontSize = "1rem", LineHeight = "1.5" },
            Body2 = new Body2Typography { FontSize = "0.875rem", LineHeight = "1.5" },
            Button = new ButtonTypography
            {
                FontSize = "0.875rem",
                FontWeight = "500",
                TextTransform = "none",
            },
            Caption = new CaptionTypography { FontSize = "0.75rem" },
        },

        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "4px",
        },
    };
}
