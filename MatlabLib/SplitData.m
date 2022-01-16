function [x,y]=SplitData(msg)
ramki=split(msg,"/");
ramki=ramki(2:end-1).';
a=split(ramki," ");
a = reshape(a,size(a,2),2);
x=a(:,2);
x= sscanf(sprintf(' %s',x{:}),'%lf',[1,Inf]);
x=x/1000000000;
y=a(:,1);
y= sscanf(sprintf(' %s',y{:}),'%f',[1,Inf]);
y=SamplesToVolts(y);
[x,y]=Interpolacja(x,y);
end