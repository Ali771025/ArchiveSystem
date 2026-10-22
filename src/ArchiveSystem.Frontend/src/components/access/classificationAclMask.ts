export const CLASSIFICATION_READ_MASK = 1;
export const CLASSIFICATION_WRITE_MASK = 2;
export const CLASSIFICATION_MOVE_MASK = 8;

const EDITABLE_CLASSIFICATION_MASK =
  CLASSIFICATION_READ_MASK |
  CLASSIFICATION_WRITE_MASK |
  CLASSIFICATION_MOVE_MASK;

export const normalizeEditableClassificationMask = (mask: number): number => {
  const editableMask = mask & EDITABLE_CLASSIFICATION_MASK;
  return (editableMask &
    (CLASSIFICATION_WRITE_MASK | CLASSIFICATION_MOVE_MASK)) !==
    0
    ? editableMask | CLASSIFICATION_READ_MASK
    : editableMask;
};

