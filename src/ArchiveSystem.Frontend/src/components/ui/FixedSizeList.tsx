import React from 'react';

interface FixedSizeListProps {
  children: (props: { index: number; style: React.CSSProperties }) => React.ReactNode;
  height: number;
  itemCount: number;
  itemSize: number;
  width: number | string;
}

const FixedSizeList: React.FC<FixedSizeListProps> = ({
  children,
  height,
  itemCount,
  itemSize,
  width,
}) => {
  const items = Array.from({ length: itemCount }, (_, index) => {
    const style: React.CSSProperties = {
      height: itemSize,
      width: '100%',
    };

    return (
      <div key={index} style={style}>
        {children({ index, style })}
      </div>
    );
  });

  return (
    <div style={{ height, width, overflowY: 'auto', overflowX: 'hidden' }}>
      {items}
    </div>
  );
};

export default FixedSizeList;

