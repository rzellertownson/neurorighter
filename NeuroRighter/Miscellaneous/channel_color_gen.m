% Jon Newman
% March 18 2011
% Generate a color array for the channels. This can then be copy pasted
% into the extension method ExtMeth01.GenerateBrainbow();

col = ceil(255*hsv(64));
str = '';
str = [str '{'];
for i = 1:size(col,1)
    str = [str '{'];
    str = [str num2str(col(i,1)) ',' num2str(col(i,2)) ',' num2str(col(i,3)) ];
    if i ~= size(col,1)
        str = [str '},\r'];
    else
        str = [str '}'];
    end
end
str = [str '}'];
sprintf(str)
