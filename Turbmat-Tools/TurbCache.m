%
% Turbmat-Tools - a Matlab library for querying, processing and visualizing
% data from the JHU Turbulence Database
%   
% TurbCache, part of Turbmat-Tools
%

%
% Written by:
% 
% Edo Frederix 
% The Johns Hopkins University / Eindhoven University of Technology 
% Department of Mechanical Engineering 
% edofrederix@jhu.edu, edofrederix@gmail.com
%

%
% This file is part of Turbmat-Tools.
% 
% Turbmat-Tools is free software: you can redistribute it and/or modify it
% under the terms of the GNU General Public License as published by the
% Free Software Foundation, either version 3 of the License, or (at your
% option) any later version.
% 
% Turbmat-Tools is distributed in the hope that it will be useful, but
% WITHOUT ANY WARRANTY; without even the implied warranty of
% MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General
% Public License for more details.
% 
% You should have received a copy of the GNU General Public License along
% with Turbmat-Tools.  If not, see <http://www.gnu.org/licenses/>.
%

classdef TurbCache < handle
    
    properties (Constant)
        % ---- Global constants ----
        cacheDir = './cachedata'
        hashAlgorithm = 'MD5'
        extension = 'cache'
    end
    
    properties
        cacheFilename
        PT
    end
    
    methods
        %
        % ---- Initiate class ----
        %
        
        % We expect the TurbTools object to be provided here
        function RC = TurbCache(PT)
            RC.PT = PT;
            
            % Check if cache directory exists. If not, create.
            if ~ exist(RC.cacheDir, 'dir')
                status = mkdir(RC.cacheDir);
                if ~ status
                    error('Could not create cache directory');
                end
            else
                % Ask the user if we want to clear the cache
                result = RC.PT.askYesno('Do you want to flush the cache?', 'No');
                if strcmp(result, 'Yes')
                    % Flush
                    fprintf('Flushing the cache\n');
                    status = rmdir(RC.cacheDir, 's');
                    if status
                        status = mkdir(RC.cacheDir);
                        if ~ status
                            error('Could not create cache directory');
                        end                        
                    end
                end
            end                
        end
        
        %
        % ---- Caching functions ----
        %
        
        % Little function to create file path and name
        function string = createCacheFilePath(RC, cacheFilename)
            string = sprintf('%s%s%s.%s', RC.cacheDir, '/', cacheFilename, RC.extension);
            RC.PT.c_cacheFile = string;
        end
        
        % This function tries to open the specified cache file, and return its 
        % value. If the cache file is not found a zero will be returned.
        function result = getCache(RC)
            cacheFile = RC.createCacheFilePath(RC.cacheFilename);
            if exist(cacheFile, 'file')
                % We found the cache file, let's try to load it
                result = importdata(cacheFile);
                fprintf('Fetching data from cache file %s\nFlush cache if you want to run a live request.\n', cacheFile);
            else
                result = [];
            end 
        end
        
        % This function saves a variable to a cache file.
        function saveCache(RC, data) %#ok<INUSD>
            cacheFile = RC.createCacheFilePath(RC.cacheFilename);
            save(cacheFile, 'data');
            fprintf('Saved data to cache file %s\n', cacheFile);
        end 
    end
end

