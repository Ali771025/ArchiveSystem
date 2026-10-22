import type { ClassificationNode } from "@/types/archive";

type ClassificationPathNode = Pick<ClassificationNode, "id" | "title" | "parentId">;

export const buildClassificationTitleMap = (
  classifications: ClassificationPathNode[],
): Map<number, string> => {
  const nodesById = new Map(classifications.map((node) => [node.id, node]));
  const titlesById = new Map<number, string>();

  const resolveTitle = (id: number, visiting: Set<number>): string => {
    const cached = titlesById.get(id);
    if (cached) {
      return cached;
    }

    const node = nodesById.get(id);
    if (!node) {
      return "";
    }

    if (visiting.has(id)) {
      return node.title;
    }

    visiting.add(id);
    const parentTitle =
      node.parentId == null ? "" : resolveTitle(node.parentId, visiting);
    visiting.delete(id);

    const fullTitle = parentTitle
      ? `${parentTitle} / ${node.title}`
      : node.title;
    titlesById.set(id, fullTitle);
    return fullTitle;
  };

  classifications.forEach((node) => {
    resolveTitle(node.id, new Set<number>());
  });

  return titlesById;
};
