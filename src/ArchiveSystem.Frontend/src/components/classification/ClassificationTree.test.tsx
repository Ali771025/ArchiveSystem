import { ThemeProvider } from '@mui/material/styles';
import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { ClassificationTree } from '@/components/classification/ClassificationTree';
import { appTheme } from '@/theme';
import type { ClassificationNode } from '@/types/archive';
import type { ComponentProps } from 'react';

const nodes: ClassificationNode[] = [
  { id: 1, title: 'Root', parentId: null, level: 0, path: '/1', caseCount: 0, isActive: true },
  { id: 2, title: 'Child', parentId: 1, level: 1, path: '/1/2', caseCount: 3, isActive: true },
];

const renderTree = (overrides?: Partial<ComponentProps<typeof ClassificationTree>>) => {
  const onSelect = vi.fn();
  const onCreate = vi.fn(async () => undefined);

  render(
    <ThemeProvider theme={appTheme}>
      <ClassificationTree
        nodes={nodes}
        selectedId={1}
        onSelect={onSelect}
        onCreate={onCreate}
        onRename={vi.fn(async () => undefined)}
        onDelete={vi.fn(async () => undefined)}
        onMove={vi.fn(async () => undefined)}
        hasCreatePermission
        hasEditPermission
        hasManagePermission
        {...overrides}
      />
    </ThemeProvider>,
  );

  return { onSelect, onCreate };
};

describe('ClassificationTree', () => {
  it('disables delete for non-removable nodes', async () => {
    renderTree();

    const deleteButtons = screen.getAllByLabelText('delete-node');
    expect(deleteButtons[0]).toBeDisabled();
  });

  it('filters by search query', async () => {
    renderTree();

    const search = screen.getAllByRole('textbox')[0];
    fireEvent.change(search, { target: { value: 'Child' } });

    expect(await screen.findByText('Child')).toBeInTheDocument();
  });

  it('supports keyboard navigation', async () => {
    const { onSelect } = renderTree();
    const tree = screen.getByRole('tree');

    tree.focus();
    fireEvent.keyDown(tree, { key: 'ArrowDown' });

    expect(onSelect).toHaveBeenCalled();
  });

  it('creates a root folder without selected parent', async () => {
    const { onCreate } = renderTree();

    fireEvent.click(screen.getByRole('button', { name: /پوشه اصلی/i }));
    fireEvent.change(screen.getByLabelText('عنوان'), { target: { value: 'Root 2' } });
    fireEvent.click(screen.getByRole('button', { name: /^ایجاد$/i }));

    expect(onCreate).toHaveBeenCalledWith('Root 2', null);
  });

  it('renders root leaf classifications with a folder icon', () => {
    const rootOnlyNodes: ClassificationNode[] = [
      { id: 10, title: 'Root Leaf', parentId: null, level: 0, path: '/10', caseCount: 0, isActive: true },
    ];

    renderTree({ nodes: rootOnlyNodes, selectedId: 10 });

    expect(screen.getByText('Root Leaf')).toBeInTheDocument();
    expect(screen.getByTestId('FolderOutlinedIcon')).toBeInTheDocument();
  });
});
