function Harmoniczne(msg,type,harmVect,dzielnik)
arguments
msg string;
type double;
harmVect double;
dzielnik double;
end
%% Ustawienia
iloscHarmonicznych=40;

%% Podział ramek z RaspberryPI
if strlength(msg)>1
[x,y]=SplitData(msg);

%% Wyznaczanie harmonicznych okresach co 0.5s
indeksStart=1;
thdLocal=[];
harmpowerLocal=[];
for i=1:1:size(x,2)
if x(i)-x(indeksStart)>=0.5

% Czestotliwość próbkowania 0.5s sygnału.
LocalSampleRate=1/1/mean(diff(x(indeksStart:i)));
    newY=y(indeksStart:i);
    
   [thd_decb,harmpower,~]= thd(newY,LocalSampleRate,40); 
% Przejscie z decybeli
thdLocal=[thdLocal 100*(10^(thd_decb/20))];
harmpowerLocal=[harmpowerLocal;(10.^(harmpower/20))']; 
  indeksStart=i;  
end
end
% Wartość średnia z całego sygnału
Thd=mean(thdLocal);
harmpow=mean(harmpowerLocal);


%% Prezentacja wyników
hold off;  
grid off;
if type==0
x1=(1:iloscHarmonicznych);
bar(x1(2:iloscHarmonicznych),harmpow(2:iloscHarmonicznych));
% title(["\color{red}THD+T: \color{black}"+Thd,"\color{red}Harmoniczna 1:  Vrms: \color{black} "+harmpow(1)+" V"]);
xlabel("Harmoniczne");
ylabel("Vrms [V]");
drawnow;
else
a=ones(1,iloscHarmonicznych)*5;

 x1=(1:iloscHarmonicznych);

bar(x1(2:iloscHarmonicznych),100*harmpow(2:iloscHarmonicznych)/harmpow(1));
% title(["\color{red}THD+T: \color{black}"+Thd,newline,"\color{red}Harmoniczna 1:  Vrms: \color{black} "+harmpow(1)+" V"]);
xlabel("Harmoniczne");
ylabel("Wartość procentowa");
if max(100*harmpow(2:iloscHarmonicznych)/harmpow(1))<7
ylim([0 7]);
else
   ylim('auto'); 
end
drawnow;   
% hold on
%  p1=plot(x1,a,'--');
% legend(p1,'Wymagania dotyczące jakości zasilania');
%  hold off
end
end
if size(harmVect,2)>1    
    
 if type==0
     harmpow=(harmVect/dzielnik);
   x1=(1:iloscHarmonicznych);
     wykres=bar(x1(2:iloscHarmonicznych),harmpow(2:iloscHarmonicznych));
     xlabel("Harmoniczne");
ylabel("Vrms [V]");
      saveas(wykres,'harmVolt','pdf')
 end
 if type==1
     
        harmpow=harmVect/dzielnik;
   x1=(1:iloscHarmonicznych);
   wykres=bar(x1(2:iloscHarmonicznych),100*harmpow(2:iloscHarmonicznych)/harmpow(1));
          xlabel("Harmoniczne");
ylabel("Wartość procentowa");
 saveas(wykres,'harmProc','pdf')
 end
 
end
end


