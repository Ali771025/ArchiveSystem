// src/services/mockData.ts
import { FileMetadata, Folder, TreeNode } from '../types';

const generateMockFolders = (): Folder[] => {
  const rootFolder: Folder = {
    id: 'root',
    name: 'Documents',
    parentId: null,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    children: []
  };

  const departments = ['HR', 'Finance', 'Engineering', 'Marketing', 'Sales'];
  const years = [2024, 2023, 2022];

  const folders: Folder[] = [rootFolder];

  departments.forEach(dept => {
    const deptFolder: Folder = {
      id: `dept_${dept.toLowerCase()}`,
      name: dept,
      parentId: 'root',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      children: []
    };
    folders.push(deptFolder);

    years.forEach(year => {
      const yearFolder: Folder = {
        id: `year_${year}_${dept.toLowerCase()}`,
        name: year.toString(),
        parentId: deptFolder.id,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
        children: []
      };
      folders.push(yearFolder);
    });
  });

  return folders;
};

const generateMockFiles = (count: number): FileMetadata[] => {
  const fileTypes = ['pdf', 'docx', 'xlsx', 'pptx', 'jpg', 'png', 'txt', 'zip'];
  const fileNames = [
    'Annual Report', 'Quarterly Review', 'Employee Handbook', 'Financial Statement',
    'Project Proposal', 'Meeting Minutes', 'Contract Agreement', 'Invoice Template',
    'Presentation Deck', 'Technical Specification'
  ];
  const owners = ['john.doe', 'jane.smith', 'mike.wilson', 'sarah.johnson', 'tom.brown'];

  const files: FileMetadata[] = [];
  
  for (let i = 0; i < count; i++) {
    const fileType = fileTypes[Math.floor(Math.random() * fileTypes.length)];
    const fileName = fileNames[Math.floor(Math.random() * fileNames.length)];
    const ownerId = owners[Math.floor(Math.random() * owners.length)];
    
    files.push({
      id: `file_${i}`,
      name: `${fileName}_${i}.${fileType}`,
      size: Math.floor(Math.random() * 10000000) + 1024, // 1KB to ~10MB
      type: fileType,
      owner: ownerId,
      createdAt: new Date(Date.now() - Math.random() * 365 * 24 * 60 * 60 * 1000).toISOString(),
      updatedAt: new Date().toISOString(),
      tags: ['important', 'reviewed', 'final'].slice(0, Math.floor(Math.random() * 3)),
      path: `/documents/${ownerId}/${Math.floor(i / 10)}`,
      parentId: `dept_${ownerId.split('.')[0]}`,
      version: 1,
      checksum: `checksum_${i}`,
      meta: {
        author: ownerId,
        category: 'document',
        language: 'en',
        wordCount: Math.floor(Math.random() * 5000)
      }
    });
  }

  return files;
};

const generateMockTreeData = (folders: Folder[], files: FileMetadata[]): TreeNode[] => {
  const buildTree = (parentId: string | null, level = 0): TreeNode[] => {
    const items = [...folders, ...files].filter(item => item.parentId === parentId);
    
    return items.map(item => {
      if ('children' in item) { // It's a folder
        return {
          ...item,
          isExpanded: level < 2, // Expand first 2 levels
          level,
          children: buildTree(item.id, level + 1)
        } as TreeNode;
      } else { // It's a file
        return {
          ...item,
          parentId: item.parentId ?? null,
          updatedAt: item.updatedAt,
          isExpanded: false,
          level,
          children: []
        } as unknown as TreeNode;
      }
    });
  };

  return buildTree(null);
};

export const mockData = {
  folders: generateMockFolders(),
  files: generateMockFiles(100),
  treeData: generateMockTreeData(generateMockFolders(), generateMockFiles(80))
};

