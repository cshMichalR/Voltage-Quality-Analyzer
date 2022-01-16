function [x,y]=Interpolacja(x,y)
DelayTime=0.0025;
k=diff(x);
for i=1:1:size(k,2)
    if k(i)>DelayTime
        if (i-2)>0 && (i+2)<size(x,2)
        try
            x(i-2:i+2)=NaN;
        y(i-2:i+2)=NaN;     
        catch
            
        end
        end
    end   
    
    if k(i)>DelayTime
        HowManyMissing=k(i)/min(k);
        EmptyWektor(1:1:HowManyMissing)=NaN;
        y1=[y(i-1) EmptyWektor y(i)];
        x1=[x(i-1) EmptyWektor x(i)];
        %% Wypełnianie przerw w sygnale
        if (i-20000>0) && (i+20000<=size(x,2))
            y1 = fillgaps([y(i-20000:i) y1 y(i:i+20000)]);
            x1 = fillgaps([x(i-20000:i) x1 x(i:i+20000)]);
        elseif (i-20000>0)
            y1 = fillgaps([y(i-40000:i) y1 y(i:size(x,2))]);
            x1 = fillgaps([x(i-40000:i) x1 x(i:size(x,2))]);
        else
            y1 = fillgaps([y(1:i) y1 y(i:i+40000)]);
            x1 = fillgaps([x(1:i) x1 x(i:i+40000)]);
        end 
    end 
    
end
%% Generowanie przerw między próbkami o równych odcinkach czasu
try
x1=[x(1:i-1) x1 x(i:end)];
y1=[y(1:i-1) y1 y(i:end)];
[y,x]=resample(y1,x1,20000,'pchip');
catch
[y,x]=resample(y,x,20000,'pchip');  
end
end

