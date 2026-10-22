/* @vitest-environment jsdom */
import "@testing-library/jest-dom/vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { InlineTagsEditorCell } from "@/components/features/InlineTagsEditorCell";

describe("InlineTagsEditorCell", () => {
  it("opens and focuses the tag input when add tag is clicked", () => {
    render(<InlineTagsEditorCell tags={[]} onSave={vi.fn()} />);

    fireEvent.click(screen.getByRole("button", { name: "افزودن تگ به سند" }));

    expect(
      screen.getByRole("textbox", { name: "ویرایش تگ‌های سند" }),
    ).toHaveFocus();
  });

  it("clearly disables the action when the user lacks permission", () => {
    render(<InlineTagsEditorCell tags={[]} disabled onSave={vi.fn()} />);

    expect(
      screen.getByRole("button", { name: "افزودن تگ به سند" }),
    ).toBeDisabled();
  });

  it("shows reusable tag options and supports multiple selection", () => {
    render(
      <InlineTagsEditorCell
        tags={[]}
        options={["شهرسازی", "مالی"]}
        onSave={vi.fn()}
      />,
    );

    fireEvent.click(screen.getByRole("button", { name: "افزودن تگ به سند" }));
    fireEvent.mouseDown(screen.getByRole("combobox"));
    expect(screen.getByText("شهرسازی")).toBeInTheDocument();
    expect(screen.getByText("مالی")).toBeInTheDocument();
  });
});
