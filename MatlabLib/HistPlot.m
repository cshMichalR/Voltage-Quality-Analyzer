function [f]=HistPlot(Value,ZakresMin,ZakresMax,Tick)

for i=1:1:size(Value,2)
    if Value(i)>ZakresMax
       Value(i)=ZakresMax;
    end
    if Value(i)<ZakresMin
       Value(i)=ZakresMin; 
    end
end

x=ZakresMin:Tick:ZakresMax;
bins=ZakresMin:Tick/2:ZakresMax; 


[N] = histcounts(Value,bins);
N1=[];
for i=2:2:size(N,2)-1
    
N1=[N1 N(i)+N(i+1)];

end
N1=[N(1) N1 N(end)];

f=bar(x(1:end), N1);
   xtickformat('%.2f')
    set(gca, 'xtick', x(1:end));
    ax=gca;
labels = string(ax.XAxis.TickLabels); % extract
labels(2:2:end) = nan; % remove every other one
ax.XAxis.TickLabels = labels; % set
         xl = get(gca,'XTickLabel');  
          new_xl = strrep(xl(:),'.',',');
          set(gca,'XTickLabel',new_xl)
end