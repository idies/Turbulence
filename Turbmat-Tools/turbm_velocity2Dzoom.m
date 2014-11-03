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


%
% ---- Initiate ----
%

clear all;
close all;
beep off;
clc;

TT = TurbTools(1);
TT.c_spatialInt = TT.LAG8_INT;
TT.i_maxQuiver = 20;

%
% ---- Ask user input ----
%

% Timestep
cl_questions = {sprintf('Enter timestep between 1 and %i (integer)', TT.TIME_OFFSET_MAX)};
cl_defaults = {'100'};
c_timeOffset = TT.askInput(cl_questions, cl_defaults);
i_timeOffset = TT.checkChar(c_timeOffset, 'int', '', [0 1024]);

% Number of timesteps to run
i_timeSteps = 1;
       
% Surface normal direction, and also compute the two surface directions
cl_questions = {sprintf('Enter surface normal direction (x, y or z)')};
cl_defaults = {'z'};
c_surfDirection = TT.checkChar(TT.askInput(cl_questions, cl_defaults), 'char', '^[xyz]{1}$');
c_directions = TT.setDirections(c_surfDirection);

% Offset position
cl_questions = {sprintf('Offset in %c', c_surfDirection)};
cl_defaults = {'1.0'};
cl_offsets = {'0.0' '0.0' TT.askInput(cl_questions, cl_defaults)};
m_offsets = TT.checkCellChars(cl_offsets, [3 3], 'float', '');

% Display relative velocity?
c_relative = TT.askYesno('Calculate relative velocity?', 'No');
c_relative = TT.checkChar(c_relative, 'char', '^Yes|No$');
if strcmp(c_relative, 'Yes'); i_relative = 1; else i_relative = 0; end

%
% ---- Construct the request ----
%

f_time = TT.TIME_SPACING * i_timeOffset;

% Gridwidth and offsets for the 4 frames
m_frameWidth = [1024 256 64 16];
m_frameOffsets = (1024-m_frameWidth)/2 * TT.SPACING;

for i_step = 1:length(m_frameWidth)
    
    % Specify step depending parameters    
    m_nPoints = ones(1,2) * m_frameWidth(i_step);
    [m_nQueryPoints m_spacing] = TT.calculateQueryPoints(m_nPoints);
    m_offsets = [m_frameOffsets(i_step) m_frameOffsets(i_step) m_offsets(3)];
    
    m_points = TT.fillRectangle(m_nQueryPoints, m_offsets, m_spacing, c_directions); 
    i_points = prod(m_nQueryPoints);
    
    cl_cacheParams = {TT.c_dataset, f_time, TT.c_spatialInt, TT.c_temporalInt, TT.c_spatialDiff, i_points, m_nQueryPoints, m_points(:,1), m_points(:,end)};
    TT.RC.cacheFilename = TT.createCacheFilename('getVelocity', cl_cacheParams);
    m_result3 =  TT.callDatabase('getVelocity', i_points, m_points, f_time, 1);
    
    % Calculate relative velocity
    if i_relative
        m_result3 = TT.calculateRelativeVelocities(m_result3);
    end      
    
    %
    % --- Plot the velocity ---
    %
    
    % Gives transposed c_directions(1) and c_directions(2) dimensions (so
    % m_X1(c_directions(2),c_directions(1)), etc)
    [m_X1 m_X2] = TT.meshgrid(m_nQueryPoints, m_offsets, m_spacing);
    
    % Gives non-transposed c_directions(1) and c_directions(2) dimensions
    % (so v_u(c_directions(1),c_directions(2)), etc)
    [v_u v_v v_w v_mag] = TT.parseVector(m_result3, m_nQueryPoints);

    if i_step == 1
        x_figure = TT.startFigure(1);
        TT.makeFigureSquare(x_figure);
    end
    
    x_subplot = subplot(2,2,i_step);
    
    % Tranpose velocity, to agree with spatial data. So:
    % v_u(c_directions(1),c_directions(2))
    v_u = transpose(v_u); v_v = transpose(v_v); v_w = transpose(v_w);
    
    TT.drawCountours(v_w, m_X1, m_X2); hold on;
    TT.drawVectormap(v_u, v_v, m_X1, m_X2);
    
    % Draw rectangle
    if i_step < 4
        k = i_step+1;
        rectangle('Position', [m_frameOffsets(k), m_frameOffsets(k), (m_frameWidth(k)*TT.SPACING), (m_frameWidth(k)*TT.SPACING)], 'LineWidth', 2.0);
    end
    
    % Style figure
    TT.setFigureAttributes('2d', {c_directions(1), c_directions(2)});
    colorbar; colormap(TT.c_colormap);
    TT.scaleSubplot(i_step, x_subplot);
    
    % Write title
    if i_step == 1
        title(x_subplot, sprintf('Velocity vector maps on %c-%c plane, at %c=%1.3f, t=%1.3f.\n Colormap indicates %c-component velocity.', c_directions, m_offsets(3), f_time, c_surfDirection), 'FontSize', 12, 'FontWeight', 'bold');        
    elseif i_step == 2
        title(x_subplot, 'Detail 1 (rectangle in top left image)', 'FontSize', 12, 'FontWeight', 'bold');
    elseif i_step == 3
        title(x_subplot, 'Detail 2 (rectangle in top right image)', 'FontSize', 12, 'FontWeight', 'bold');
    elseif i_step == 4
        title(x_subplot, 'Detail 3 (rectangle in bottom left image)', 'FontSize', 12, 'FontWeight', 'bold');
    end

end
