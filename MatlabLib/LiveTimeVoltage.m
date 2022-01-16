function LiveTimeVoltage(msg,time,save)

arguments
    msg string;
    time double;
    save double;
end
[x,y]=SplitData(msg);


if x(end)-x(1)>time
    try
        
        [pks, locs] = findpeaks(y,x,'MinPeakHeight',300,'MinPeakDistance',0.01);
    catch
        for i=1:1:size(x,2)
            if x(i)-x(1)>=time
                wykres=plot(x(1:i)-x(1),y(1:i));
            end
        end
    end
    if size(pks,2)>0
        for j=1:1:size(x,2)
            if x(j)==locs(1) && y(j)==pks(1)
                x=x(j:end);
                y=y(j:end);
                break;
            end
        end
        
        
        for i=1:1:size(x,2)
            if x(i)-x(1)>=time
                wykres=plot(x(1:i)-x(1),y(1:i));
                xlabel("Czas [s]");
                ylabel("Napięcie [V]");
                axis([0 time -350,350]);
                grid on
                drawnow;
                break;
            end
        end
    end
    
end
xl = get(gca,'XTickLabel');
new_xl = strrep(xl(:),'.',',');
set(gca,'XTickLabel',new_xl)
if save==1
    saveas(wykres,'oscylogram','pdf')
end

end