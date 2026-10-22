import LocalOfferOutlinedIcon from "@mui/icons-material/LocalOfferOutlined";
import CheckIcon from "@mui/icons-material/Check";
import CloseIcon from "@mui/icons-material/Close";
import { Autocomplete, Chip, IconButton, Stack, TextField, Typography } from "@mui/material";
import { useEffect, useMemo, useRef, useState } from "react";
import { cleanTagName, normalizeTagName } from "@/utils/tagNormalization";

interface InlineTagsEditorCellProps {
  tags: string[];
  options?: string[];
  disabled?: boolean;
  forceEditing?: boolean;
  onEditEnd?: () => void;
  onSave: (tags: string[]) => Promise<void> | void;
}

const normalizeTags = (values: string[]): string[] => {
  const seen = new Set<string>();
  return values
    .map(cleanTagName)
    .filter(Boolean)
    .filter((item) => {
      const key = normalizeTagName(item);
      if (seen.has(key)) {
        return false;
      }

      seen.add(key);
      return true;
    });
};

export const InlineTagsEditorCell = ({
  tags,
  options = [],
  disabled = false,
  forceEditing = false,
  onEditEnd,
  onSave,
}: InlineTagsEditorCellProps) => {
  const [editing, setEditing] = useState(false);
  const [draft, setDraft] = useState<string[]>([]);
  const [saving, setSaving] = useState(false);
  const inputRef = useRef<HTMLInputElement | null>(null);

  const label = useMemo(() => tags.join(", "), [tags]);

  const startEditing = () => {
    if (disabled) {
      return;
    }

    setDraft(tags);
    setEditing(true);
    requestAnimationFrame(() => {
      inputRef.current?.focus();
    });
  };

  useEffect(() => {
    if (!editing) {
      setDraft(tags);
    }
  }, [editing, label]);

  useEffect(() => {
    if (forceEditing && !disabled) {
      setEditing(true);
    setDraft(tags);
      requestAnimationFrame(() => {
        inputRef.current?.focus();
      });
    }
  }, [disabled, forceEditing, tags]);

  const commit = async () => {
    if (disabled) {
      setEditing(false);
      onEditEnd?.();
      return;
    }

    const normalized = normalizeTags(draft);
    setSaving(true);
    try {
      await onSave(normalized);
      setEditing(false);
      onEditEnd?.();
    } catch {
      // Keep editor open so the user can adjust the value when save fails.
    } finally {
      setSaving(false);
    }
  };

  if (editing) {
    return (
      <Stack direction="row" spacing={0.5} alignItems="center" sx={{ width: "100%" }} onClick={(event) => event.stopPropagation()}>
        <Autocomplete
          multiple
          freeSolo
          openOnFocus
          autoHighlight
          disabled={saving}
          options={options}
          value={draft}
          filterSelectedOptions
          isOptionEqualToValue={(option, value) => normalizeTagName(option) === normalizeTagName(value)}
          onChange={(_event, value) => setDraft(normalizeTags(value))}
          renderInput={(params) => (
            <TextField
              {...params}
              autoFocus
              inputRef={inputRef}
              aria-label="ویرایش تگ‌های سند"
              placeholder={draft.length === 0 ? "جستجو یا ایجاد تگ..." : undefined}
              size="small"
            />
          )}
          sx={{ flex: 1, minWidth: 220 }}
        />
        <IconButton size="small" color="primary" aria-label="ذخیره تگ‌ها" disabled={saving} onClick={() => void commit()}>
          <CheckIcon fontSize="small" />
        </IconButton>
        <IconButton size="small" aria-label="لغو ویرایش تگ‌ها" disabled={saving} onClick={() => { setEditing(false); onEditEnd?.(); }}>
          <CloseIcon fontSize="small" />
        </IconButton>
      </Stack>
    );
  }

  if (tags.length === 0) {
    return (
      <Typography
        component="button"
        type="button"
        disabled={disabled}
        aria-label="افزودن تگ به سند"
        title={disabled ? "شما مجوز افزودن تگ به سند را ندارید." : undefined}
        variant="caption"
        color="text.secondary"
        sx={{
          appearance: "none",
          border: 0,
          background: "transparent",
          p: 0,
          fontFamily: "inherit",
          cursor: disabled ? "not-allowed" : "pointer",
          fontStyle: "italic",
          textDecoration: disabled ? "none" : "underline",
          textUnderlineOffset: 3,
        }}
        onClick={(event) => {
          event.stopPropagation();
          startEditing();
        }}
      >
        افزودن تگ...
      </Typography>
    );
  }

  return (
    <Stack
      direction="row"
      spacing={0.5}
      flexWrap="wrap"
      onClick={(event) => {
        event.stopPropagation();
        startEditing();
      }}
      sx={{
        cursor: disabled ? "default" : "text",
        minWidth: 0,
        maxWidth: "100%",
        alignItems: "center",
      }}
    >
      {tags.map((tag) => (
        <Chip
          key={tag}
          size="small"
          variant="outlined"
          label={tag}
          icon={
            <LocalOfferOutlinedIcon sx={{ fontSize: "0.95rem !important" }} />
          }
          sx={{
            maxWidth: 160,
            height: 22,
            "& .MuiChip-label": {
              px: 0.75,
              fontSize: "0.68rem",
              lineHeight: 1.3,
            },
          }}
        />
      ))}
    </Stack>
  );
};
