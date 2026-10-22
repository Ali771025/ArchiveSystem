import type {} from "@mui/x-data-grid/themeAugmentation";
import type { GridLocaleText } from "@mui/x-data-grid";
import { createTheme, type PaletteMode } from "@mui/material/styles";

const breakpoints = {
  values: {
    xs: 0,
    sm: 480,
    md: 768,
    lg: 1024,
    xl: 1280,
  },
};

const typography = {
  fontFamily: '"Vazirmatn", Tahoma, "Segoe UI", Arial, sans-serif',
  h1: {
    fontSize: "clamp(1.9rem, 3vw, 2.6rem)",
    fontWeight: 700,
  },
  h2: {
    fontSize: "clamp(1.65rem, 2.6vw, 2.2rem)",
    fontWeight: 700,
  },
  h3: {
    fontSize: "clamp(1.45rem, 2.2vw, 1.95rem)",
    fontWeight: 700,
  },
  h4: {
    fontWeight: 700,
    fontSize: "clamp(1.3rem, 1.9vw, 1.7rem)",
  },
  h5: {
    fontWeight: 700,
    fontSize: "clamp(1.05rem, 1.5vw, 1.32rem)",
  },
  h6: {
    fontWeight: 700,
    fontSize: "clamp(1rem, 1.3vw, 1.15rem)",
  },
  body1: {
    fontSize: "clamp(0.92rem, 0.9vw, 1rem)",
    lineHeight: 1.65,
  },
  body2: {
    fontSize: "clamp(0.82rem, 0.85vw, 0.92rem)",
    lineHeight: 1.55,
  },
};

const toPersianNumber = (value: number) => value.toLocaleString("fa-IR");

const dataGridLocaleText: Partial<GridLocaleText> = {
  paginationRowsPerPage: "تعداد ردیف در صفحه",
  paginationDisplayedRows: ({ from, to, count, estimated }) => {
    const total =
      estimated && estimated > to
        ? `بیش از ${toPersianNumber(to)}`
        : count !== -1
          ? toPersianNumber(count)
          : `بیش از ${toPersianNumber(to)}`;

    return `ردیف‌های ${toPersianNumber(from)} تا ${toPersianNumber(to)} از ${total}`;
  },
};

export const createAppTheme = (mode: PaletteMode = "light") => {
  const isDark = mode === "dark";
  const backgroundDefault = isDark ? "#1F2937" : "#F3F6FA";
  const backgroundPaper = isDark ? "#273549" : "#FFFFFF";
  const appTextPrimary = isDark ? "#F9FAFB" : "#0B1320";
  const appTextSecondary = isDark ? "#CBD5E1" : "#475569";
  const appDivider = isDark
    ? "rgba(203, 213, 225, 0.18)"
    : "rgba(15, 23, 42, 0.12)";

  return createTheme({
    direction: "rtl",
    spacing: 8,
    breakpoints,
    shape: {
      borderRadius: 10,
    },
    palette: {
      mode,
      primary: {
        main: "#14B8A6",
        light: "#2DD4BF",
        dark: "#0F766E",
        contrastText: "#0B1320",
      },
      secondary: {
        main: "#2563EB",
        light: "#60A5FA",
        dark: "#1D4ED8",
        contrastText: "#FFFFFF",
      },
      background: {
        default: backgroundDefault,
        paper: backgroundPaper,
      },
      text: {
        primary: appTextPrimary,
        secondary: appTextSecondary,
      },
      divider: appDivider,
      action: {
        hover: isDark ? "rgba(45, 212, 191, 0.12)" : "rgba(20, 184, 166, 0.08)",
        selected: isDark
          ? "rgba(45, 212, 191, 0.18)"
          : "rgba(20, 184, 166, 0.14)",
      },
      error: {
        main: "#DC2626",
      },
      warning: {
        main: "#F59E0B",
      },
      success: {
        main: "#16A34A",
      },
      info: {
        main: "#2563EB",
      },
    },
    typography,
    components: {
      MuiCssBaseline: {
        styleOverrides: {
          ":root": {
            colorScheme: mode,
            backgroundColor: backgroundDefault,
          },
          body: {
            backgroundColor: backgroundDefault,
            transition: "background-color 180ms ease, color 180ms ease",
          },
        },
      },
      MuiContainer: {
        styleOverrides: {
          root: {
            width: "100%",
            "@media (min-width: 1440px)": {
              maxWidth: "1400px",
              marginInline: "auto",
            },
          },
        },
      },
      MuiButton: {
        defaultProps: {
          size: "small",
        },
        styleOverrides: {
          root: {
            textTransform: "none",
            fontWeight: 600,
            minHeight: 40,
            paddingInline: 14,
          },
        },
      },
      MuiTextField: {
        defaultProps: {
          size: "small",
        },
      },
      MuiInputBase: {
        styleOverrides: {
          root: {
            minHeight: 40,
            color: appTextPrimary,
            backgroundColor: backgroundPaper,
          },
          input: {
            "&::placeholder": {
              color: isDark ? "#94a3b8" : "#64748b",
              opacity: 1,
            },
          },
        },
      },
      MuiInputLabel: {
        styleOverrides: {
          root: {
            color: appTextSecondary,
          },
        },
      },
      MuiOutlinedInput: {
        styleOverrides: {
          notchedOutline: {
            borderColor: isDark
              ? "rgba(203, 213, 225, 0.26)"
              : "rgba(15, 23, 42, 0.18)",
          },
        },
      },
      MuiMenu: {
        styleOverrides: {
          paper: {
            backgroundColor: backgroundPaper,
            color: appTextPrimary,
          },
        },
      },
      MuiTableCell: {
        styleOverrides: {
          root: {
            color: appTextPrimary,
            borderColor: appDivider,
          },
          head: {
            backgroundColor: isDark ? "#1F2937" : "#F8FAFC",
            fontWeight: 700,
          },
        },
      },
      MuiAutocomplete: {
        styleOverrides: {
          paper: {
            backgroundColor: backgroundPaper,
            color: appTextPrimary,
            border: `1px solid ${isDark ? "rgba(148, 163, 184, 0.24)" : "rgba(15, 118, 110, 0.14)"}`,
          },
          option: {
            '&[aria-selected="true"]': {
              backgroundColor: isDark
                ? "rgba(45, 212, 191, 0.18)"
                : "rgba(15, 118, 110, 0.10)",
            },
            "&.Mui-focused": {
              backgroundColor: isDark
                ? "rgba(45, 212, 191, 0.12)"
                : "rgba(15, 118, 110, 0.08)",
            },
          },
          noOptions: {
            color: appTextSecondary,
          },
          loading: {
            color: appTextSecondary,
          },
        },
      },
      MuiDataGrid: {
        defaultProps: {
          localeText: dataGridLocaleText,
        },
        styleOverrides: {
          root: {
            color: appTextPrimary,
            borderColor: appDivider,
            backgroundColor: backgroundPaper,
            direction: "rtl",
            "& .MuiDataGrid-columnHeaders, & .MuiDataGrid-virtualScroller, & .MuiDataGrid-footerContainer":
              {
                direction: "rtl",
              },
            "& .MuiDataGrid-cell": {
              textAlign: "right",
            },
            "& .MuiDataGrid-columnHeaderTitleContainer": {
              justifyContent: "flex-start",
            },
            "& .MuiDataGrid-cell--textCenter": {
              textAlign: "center",
            },
            "& .MuiDataGrid-columnHeader--alignCenter .MuiDataGrid-columnHeaderTitleContainer":
              {
                justifyContent: "center",
              },
          },
          columnHeaders: {
            backgroundColor: isDark ? "#1F2937" : "#F8FAFC",
            color: appTextPrimary,
          },
          columnHeader: {
            "&:focus, &:focus-within": {
              outlineColor: "#14B8A6",
            },
          },
          cell: {
            borderColor: appDivider,
            "&:focus, &:focus-within": {
              outlineColor: "#14B8A6",
            },
          },
          row: {
            "&:hover": {
              backgroundColor: isDark
                ? "rgba(45, 212, 191, 0.08)"
                : "rgba(15, 118, 110, 0.05)",
            },
            "&.Mui-selected": {
              backgroundColor: isDark
                ? "rgba(45, 212, 191, 0.14)"
                : "rgba(15, 118, 110, 0.10)",
            },
          },
          footerContainer: {
            borderColor: appDivider,
            color: appTextPrimary,
          },
        },
      },
      MuiDialog: {
        styleOverrides: {
          paper: {
            width: "min(100%, 56rem)",
            margin: 12,
            "@media (min-width: 768px)": {
              margin: 16,
            },
          },
        },
      },
      MuiPaper: {
        styleOverrides: {
          root: {
            backgroundImage: "none",
          },
        },
      },
    },
  });
};

export const appTheme = createAppTheme("light");
