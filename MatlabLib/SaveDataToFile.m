function SaveDataToFile(msg,nazwa)
arguments
   msg,nazwa string; 
end
[x,y]=SplitData(msg)
s = strcat(nazwa,'.mat');
filename = s;
save(filename)

end