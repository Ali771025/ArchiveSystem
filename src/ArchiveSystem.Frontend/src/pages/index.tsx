// src/pages/index.tsx
import React, { useEffect } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';
import { useFileStore } from '@/stores/useFileStore';
import { FileText, Database, Users, TrendingUp } from 'lucide-react';

const DashboardPage = () => {
  const { initializeMockData, files, folders } = useFileStore();

  useEffect(() => {
    initializeMockData();
  }, []);

  const stats = [
    {
      title: 'Total Files',
      value: files.length.toLocaleString(),
      icon: FileText,
      change: '+12%',
      color: 'text-blue-600'
    },
    {
      title: 'Storage Used',
      value: '2.4 TB',
      icon: Database,
      change: '+5%',
      color: 'text-green-600'
    },
    {
      title: 'Active Users',
      value: '1,248',
      icon: Users,
      change: '+8%',
      color: 'text-purple-600'
    },
    {
      title: 'Upload Rate',
      value: '24/hour',
      icon: TrendingUp,
      change: '+3%',
      color: 'text-orange-600'
    }
  ];

  const chartData = [
    { day: 'Mon', uploads: 45 },
    { day: 'Tue', uploads: 52 },
    { day: 'Wed', uploads: 48 },
    { day: 'Thu', uploads: 61 },
    { day: 'Fri', uploads: 55 },
    { day: 'Sat', uploads: 38 },
    { day: 'Sun', uploads: 42 }
  ];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight sm:text-3xl">Dashboard</h1>
        <p className="text-muted-foreground">Welcome back! Here's what's happening today.</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {stats.map((stat, index) => (
          <Card key={index}>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">{stat.title}</CardTitle>
              <stat.icon className={`h-5 w-5 ${stat.color}`} />
            </CardHeader>
            <CardContent>
              <div className="text-xl font-bold sm:text-2xl">{stat.value}</div>
              <p className="text-xs text-muted-foreground mt-1">{stat.change} from last week</p>
            </CardContent>
          </Card>
        ))}
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Weekly Upload Activity</CardTitle>
        </CardHeader>
        <CardContent>
          <ResponsiveContainer width="100%" height={300}>
            <BarChart data={chartData}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="day" />
              <YAxis />
              <Tooltip />
              <Bar dataKey="uploads" fill="#3b82f6" radius={[4, 4, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </CardContent>
      </Card>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card>
          <CardHeader>
            <CardTitle>Recent Files</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {files.slice(0, 5).map(file => (
                <div key={file.id} className="flex flex-col gap-1 p-2 hover:bg-accent rounded sm:flex-row sm:items-center sm:justify-between">
                  <div>
                    <div className="font-medium">{file.name}</div>
                    <div className="text-sm text-muted-foreground">{file.owner}</div>
                  </div>
                  <div className="text-sm text-muted-foreground">
                    {new Date(file.createdAt).toLocaleDateString()}
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Top Folders</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {folders.slice(0, 5).map(folder => (
                <div key={folder.id} className="flex items-center justify-between gap-2 p-2 hover:bg-accent rounded">
                  <div className="font-medium">{folder.name}</div>
                  <div className="text-sm text-muted-foreground">
                    {folder.children.length} items
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
};

export default DashboardPage;

