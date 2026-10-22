// @vitest-environment jsdom

import { ThemeProvider } from "@mui/material/styles";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { DynamicCaseFilters } from "@/components/cases/DynamicCaseFilters";
import { appTheme } from "@/theme";
import type {
  DynamicFilterExpression,
  DynamicTemplate,
} from "@/types/archive";

const template: DynamicTemplate = {
  id: "template-1",
  classificationId: 10,
  version: 1,
  publishedAt: new Date().toISOString(),
  groups: [
    {
      id: "group-1",
      title: "اطلاعات ملک",
      apiName: "property",
      order: 0,
      fields: [
        {
          id: "renovation-code",
          apiName: "renovationCode",
          label: "کد نوسازی ملک",
          type: "text",
          required: false,
          showAsFilter: true,
          filterType: "contains",
          order: 0,
          options: [],
          rules: {},
        },
      ],
    },
  ],
};

vi.mock("@/services/archiveQueries", () => ({
  useEffectiveDynamicTemplate: () => ({
    data: template,
    isLoading: false,
    isError: false,
  }),
}));

describe("DynamicCaseFilters", () => {
  it("restores an applied expression so its conditions remain editable", async () => {
    const onApply = vi.fn();
    const value: DynamicFilterExpression = {
      logic: "and",
      groups: [
        {
          logic: "and",
          filters: [
            {
              fieldId: "renovation-code",
              op: "contains",
              value: "12345",
            },
          ],
        },
      ],
    };

    render(
      <ThemeProvider theme={appTheme}>
        <DynamicCaseFilters
          classificationId={10}
          value={value}
          onApply={onApply}
          onClose={vi.fn()}
        />
      </ThemeProvider>,
    );

    expect(await screen.findByDisplayValue("12345")).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "اعمال فیلتر" }));

    await waitFor(() => {
      expect(onApply).toHaveBeenCalledWith(value);
    });
    expect(screen.getByDisplayValue("12345")).toBeInTheDocument();
  });
});
