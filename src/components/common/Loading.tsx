interface LoadingProps {
  text?: string;
}

const Loading = ({ text = 'Đang tải...' }: LoadingProps) => {
  return (
    <div className="flex items-center justify-center min-h-[200px]">
      <div className="flex flex-col items-center gap-4">
        <div className="w-10 h-10 border-4 border-blue-200 border-t-blue-600 rounded-full animate-spin"></div>
        <p className="text-gray-500 text-sm">{text}</p>
      </div>
    </div>
  );
};

export default Loading;
