function [Vpeak,Vrms,Freq,Time,Thd,DeltaU,ZapadTime,ZapadValue,DeltaHz,PeaksFlicker,Pst,HarmVect] = DisplayMeasurments(msg,timeApp,PeaksFlicker,HarmVect)
arguments
    msg string;
    timeApp string;
    PeaksFlicker double;
    HarmVect double;
    
end
%% Vrms znamionowe
Vrmsznam=230;
%% Podział ramek z RaspberryPI
[x,y]=SplitData(msg);
%% Częstotliwość próbkowania
SampleRate=1/1/mean(diff(x));
disp("Czas: "+(x(end)-x(1)));
%% Thd
% Podzial sygnalu na 0.5 sekundowe części
indeksStart=1;
thdLocal=[];
harmpowerLocal=[];
for i=1:1:size(x,2)
    if x(i)-x(indeksStart)>=0.5
        % Czestotliwość próbkowania
        LocalSampleRate=1/1/mean(diff(x(indeksStart:i)));
        newY=y(indeksStart:i);
        
        [thd_db,harmpower,~]= thd(newY,LocalSampleRate,40);
        % Thd procent
        thdLocal=[thdLocal 100*(10^(thd_db/20))];
        harmpowerLocal=[harmpowerLocal;(10.^(harmpower/20))'];
        indeksStart=i;
    end
end
Thd=mean(thdLocal);
harmpowerLocal=mean(harmpowerLocal);
try
    HarmVect=HarmVect+harmpowerLocal;
catch
    HarmVect=harmpowerLocal;
end
%% Aktualny czas sygnału
localTime=x(end)-x(1);
duracja=duration([0 0 localTime]);
date=datetime(timeApp,'InputFormat','dd-MMM-yyyy HH:mm:ss.SSS');
Time=datestr(date+duracja,'dd-mmm-yyyy HH:MM:SS.FFF');
%% Czestotliwość sygnału
indeksStart=1;
xStart=x(indeksStart);
licznik=1;
freqLocal=[];
for i=1:1:size(x,2)
    if x(i)-xStart>=0.5
        newX=x(indeksStart:i);
        newY=y(indeksStart:i);
        L=size(newY,2);
        Y=fft(newY,size(newY,2)*100);
        Fs=1/mean(diff(newX));
        P2 = abs(Y/L);
        P1 = P2(1:floor((L/2)+1));
        f = Fs*(0:(L/2))/L/100;
        [Pks,Locs]=findpeaks(P1);
        [~,ind] = max(Pks);
        Indeks=Locs(ind);
        freqLocal=[freqLocal, f(Indeks)];
        indeksStart=i;
        % licznik=licznik+1;
        xStart=x(i);
    end
end
Freq=mean(freqLocal);
%% Wykrywanie szczytów
[Pks,Locs]=findpeaks(y,SampleRate,'MinPeakDistance',0.01,'MinPeakHeight',Vrmsznam*0.1);
% Kasowanie pierwszego i ostatniego szczytu z sygnału
Pks=Pks(2:end-1);
Locs=Locs(2:end-1);
%% Wartość szczytowa
Vpeak=mean(Pks);
%% Wartość skuteczna
Pksvrms=Pks/sqrt(2);
Vrms=mean(Pksvrms);
%% Detekcja zapadów zasilania <90% >1%
ZapadTime=[];
ZapadValue=[];
zapadCount=0;
for i=1:1:size(Pksvrms,2)
    if Pksvrms(i)<(Vrmsznam*0.9) && Pksvrms(i)>(Vrmsznam*0.01)
        zapadCount=zapadCount+1;
    else
        if zapadCount==1
            ZapadTime=0.02;
            ZapadValue=[ZapadValue,Vrmsznam-Pksvrms(i-1)];
            zapadCount=0;
        end
        if zapadCount>1
            ZapadTime=[ZapadTime,sum(diff(Locs((i-zapadCount):i)))];
            ZapadValue=[ZapadValue,(Vrmsznam-mean(Pksvrms((i-zapadCount):i-1)))];
            zapadCount=0;
        end
        
    end
end
if size(ZapadTime,1)<1
    ZapadTime=NaN;
    ZapadValue=NaN;
end
%% Współczynnik migotania światła
deltaU=[];
PeaksFlicker=[PeaksFlicker Pksvrms];

bins=0.5:0.5:10;
F=0.8;
if size(PeaksFlicker,2)>=3000
    for i=1:1:size(PeaksFlicker,2)
        deltaU=[deltaU,abs(Vrms-PeaksFlicker(i))];
    end
    dvals = sort(deltaU, 'descend');
    [N] = histcounts(dvals,bins);
    k = find(N);
    k=sort(k, 'descend');
    
    spadek=mean(dvals(1:N(k(1))));
    tf=(2.3*(F*(100*spadek/Vrms)).^(3.2))*N(k(1));
    Pst=(sum(tf)/(size(PeaksFlicker,2))*0.02)^(1/3.2);
    if Pst>1
        save('june10');
    end
    PeaksFlicker=[];
else
    Pst=NaN;
end
%% Zaburzenia napięcia
DeltaU=100*Vrms/Vrmsznam;
%% Zaburzenia częstliwości
DeltaHz=100*Freq/50;
end