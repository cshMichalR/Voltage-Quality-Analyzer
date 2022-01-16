function[wykres]= makePlot(timeMemory,dataMemory,type,wykres,save)
%% 0 clear plot, 1 freq(t), 2 vrms(t)
arguments
    timeMemory;
    dataMemory,type double;
    wykres;
    save double;
end



switch type
    case 0
        
        figure('PaperPosition',[.5 18 20 12]);
        %% Czestotliwość czas
    case 1
        
        if size(dataMemory,2)>1
            timeMemory = datetime(timeMemory,'InputFormat','dd-MMM-yyyy HH:mm:ss.SSS');
            wykres=plot(timeMemory,dataMemory ,'g');
            % set(gca,'Color','Black')
            xtickformat('HH:mm:ss')
            
        else
            try
                
                wykres=plot(timeMemory,dataMemory,'g');
                % set(gca,'Color','Black')
                xtickformat('HH:mm:ss')
                
            catch
                
                
            end
  
        end
                yl = get(gca,'YTickLabel');  
          new_yl = strrep(yl(:),'.',',');
          set(gca,'YTickLabel',new_yl)
         grid on
        xlabel("Time [s]");
        ylabel("Frequency [Hz]");
        if save==1
            saveas(wykres,'freqTime','pdf')
        end
       
        %% Vrms czas
    case 2
        
        
        if size(dataMemory,2)>1
            timeMemory = datetime(timeMemory,'InputFormat','dd-MMM-yyyy HH:mm:ss.SSS');
            wykres=plot(timeMemory,dataMemory ,'g');
            % set(gca,'Color','Black')
            xtickformat('HH:mm:ss')
            
        else
            try
                
                wykres=plot(timeMemory,dataMemory,'g');
                % set(gca,'Color','Black')
                xtickformat('HH:mm:ss')
                
            catch
                
                
            end
            
            
            
        end
                  yl = get(gca,'YTickLabel');  
          new_yl = strrep(yl(:),'.',',');
          set(gca,'YTickLabel',new_yl)
          grid on
        xlabel("Time [s]");
        ylabel("V_r_m_s  [V]");
        if save==1
            saveas(wykres,'vrmsTime','pdf')
        end
      
    case 3
        %% Histogram częstotliwości
        grid off
        w=HistPlot(dataMemory,49.8,50.2,0.02);
        xlabel("Częstotliwość [Hz]");
        ylabel("Liczebność");
       
        if(save==1)
            saveas(w,'freqHist','pdf')
        end
    case 4
        %% Histogram napięcia
        grid off
        w=HistPlot(dataMemory,226.5,233.5,0.5);
        xlabel("V_r_m_s [V]");
        ylabel("Liczebność");
      
        if save==1
            saveas(w,'vrmsHist','pdf')
        end
    case 5
        %% Thd w czasie
        if size(dataMemory,2)>1
            timeMemory = datetime(timeMemory,'InputFormat','dd-MMM-yyyy HH:mm:ss.SSS');
            wykres=plot(timeMemory,dataMemory ,'g');
            % set(gca,'Color','Black')
            xtickformat('HH:mm:ss')
            
        else
            try
                
                wykres=plot(timeMemory,dataMemory,'g');
                % set(gca,'Color','Black')
                xtickformat('HH:mm:ss')
                
            catch
                
                
            end
            
            
            
        end
          grid on
        xlabel("Time [s]");
        ylabel("THD [%]");
                yl = get(gca,'YTickLabel');  
          new_yl = strrep(yl(:),'.',',');
          set(gca,'YTickLabel',new_yl)
        if save==1
            saveas(wykres,'thdTime','pdf')
        end
      
    case 6
        %% Zaburzenia napiecia
        
        if size(dataMemory,2)>1
            timeMemory = datetime(timeMemory,'InputFormat','dd-MMM-yyyy HH:mm:ss.SSS');
            wykres=plot(timeMemory,dataMemory ,'g');
            % set(gca,'Color','Black')
            xtickformat('HH:mm:ss')
            
        else
            try
                
                wykres=plot(timeMemory,dataMemory,'g');
                % set(gca,'Color','Black')
                xtickformat('HH:mm:ss')
                
            catch
                
                
            end
            
            
            
        end
                   yl = get(gca,'YTickLabel');  
          new_yl = strrep(yl(:),'.',',');
          set(gca,'YTickLabel',new_yl)    
        xlabel("Time [s]");
      ylabel('$\frac{V_{rms}}{230}${100\%}','Interpreter','latex','FontSize',18)
        grid on
        if save==1
            saveas(wykres,'deltaUTime','pdf')
        end
        
        
    case 7
        %% Zaburzenia częstotliwości
        if size(dataMemory,2)>1
            timeMemory = datetime(timeMemory,'InputFormat','dd-MMM-yyyy HH:mm:ss.SSS');
            wykres=plot(timeMemory,dataMemory ,'g');
            % set(gca,'Color','Black')
            xtickformat('HH:mm:ss')
            
        else
            try
                
                wykres=plot(timeMemory,dataMemory,'g');
                % set(gca,'Color','Black')
                xtickformat('HH:mm:ss')
                
            catch
                
                
            end
            
            
            
        end
        grid on
        xlabel("Time [s]");
        ylabel('$\frac{Freq}{50}${100\%}','Interpreter','latex','FontSize',14)
                yl = get(gca,'YTickLabel');  
          new_yl = strrep(yl(:),'.',',');
          set(gca,'YTickLabel',new_yl)
        if save==1
            saveas(wykres,'deltaHzTime','pdf')
        end
        
        
    case 8
        %% Flicker czas
        if size(dataMemory,2)>1
            timeMemory = datetime(timeMemory,'InputFormat','dd-MMM-yyyy HH:mm:ss.SSS');
            wykres=plot(timeMemory,dataMemory ,'g');
            % set(gca,'Color','Black')
            xtickformat('HH:mm:ss')
            
        else
            try
                
                wykres=plot(timeMemory,dataMemory,'g');
                % set(gca,'Color','Black')
                xtickformat('HH:mm:ss')
                
            catch
                
                
            end
            
            
            
        end
             yl = get(gca,'YTickLabel');  
          new_yl = strrep(yl(:),'.',',');
          set(gca,'YTickLabel',new_yl)   
         grid on
        xlabel("Time [s]");
        ylabel("P_s_t")
        if save==1
            saveas(wykres,'flickerTime','pdf')
        end
       
end


drawnow;
end



