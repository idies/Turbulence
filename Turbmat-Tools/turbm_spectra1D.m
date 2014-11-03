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
% Modified by:
%
% Jason Graham
% The Johns Hopkins University
% Department of Mechanical Engineering
% jgraha8@gmail.com
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

TT = TurbTools(0);

%
% ---- User input ----
%

% Timestep
cl_questions = {sprintf('Enter timestep between 1 and %i (integer)', TT.TIME_OFFSET_MAX)};
cl_defaults = {'100'};
c_timeOffset = TT.askInput(cl_questions, cl_defaults);
i_timeOffset = TT.checkChar(c_timeOffset, 'int', '', [0 1024]);

% Number of lines
cl_questions = {sprintf('Enter number of randomly generated lines')};
cl_defaults = {'6'};
c_lines = TT.askInput(cl_questions, cl_defaults);
i_lines = TT.checkChar(c_lines, 'int', '', [0 Inf]);   

%
% ---- Construct the request ----
%

f_time = TT.TIME_SPACING * i_timeOffset;
[s_lines i_points] = TT.createLines(i_lines);

m_points = TT.fillLines(s_lines);
m_result3 = TT.callDatabase('getVelocity', i_points, m_points, f_time, 0);

fprintf('Selecting %i random lines at t=%1.4f\n', i_lines, f_time);

%
% ---- Calculate spectra ----
%

s_inlineVel = TT.parseLines(m_result3, s_lines);
s_inlineVel = TT.calculateZeroMean(s_inlineVel);

[dft pwr k n] = TT.calculateFFTLines(s_inlineVel);
output = TT.calculateStatProperties(s_inlineVel);

fprintf('Mean inline velocity: %1.4f. Variance: %1.4f. Mean squared: %1.4f. Standard deviation: %1.4f\n', output);

%
% ---- Plot all signals ----
%

x_figure = TT.startFigure(1);
subplot(2,10,[2 9]);

keys = fieldnames(s_inlineVel);
x = linspace(0,2*pi,1024);
m_colors = TT.createColors(i_lines);
for i = 1:numel(keys)
    key = char(keys(i));
    plot(x, s_inlineVel.(key), 'LineWidth', 1.3, 'Color', m_colors(i,:));
    hold on;
end

% Style figure
grid;
title(sprintf('Inline velocity for %i random lines', i_lines), 'FontSize', 12, 'FontWeight', 'bold');
TT.setFigureAttributes('1d', {'x', 'v'});
% Adjust axis from default
xlim([0.0 2*pi]);

%
% ---- Plot random lines in space ----
%

subplot(2,10,[17 20]);
keys = fieldnames(s_lines);
for i = 1:numel(keys);
    key = char(keys(i));
    plot3(s_lines.(key).x, s_lines.(key).y, s_lines.(key).z, 'Color', m_colors(i,:), 'LineWidth', 1.3);
    hold on;
end

title(sprintf('Randomly chosen lines in domain at t=%1.3f', f_time), 'FontSize', 12, 'FontWeight', 'bold');
TT.setFigureAttributes('3d', {'x', 'y', 'z'});

%
% ---- Plot power spectrum ----
%

[kEta E] = TT.scaleEnergySpectrum(k(1:n/2+1), pwr(1:n/2+1));
subplot(2,10,[11 16]);
%subplot(2,2,[2 4]);
x_plot = loglog(kEta, E/1024);

set(x_plot, 'Color', 'r', 'LineWidth', 1.3);

hold on;
kEta2 = (2:100).*TT.KOLMOGOROV_LENGTH;
EIS = 18/55 * 1.6 * kEta2.^(-5/3);
plot(kEta2, EIS, 'b', 'LineWidth', 1.6);

% Style figure
grid;
text(kEta2(20)*2, EIS(20)*2, 'E_{11} = (18/55) * 1.6 * (k\eta)^{-5/3}');
title('Power spectrum', 'FontSize', 12, 'FontWeight', 'bold');
ylabel('E_{11}/(\epsilon\nu^5)^{1/4}', 'FontSize', 12, 'FontWeight', 'bold');
xlabel('k\eta', 'FontSize', 12, 'FontWeight', 'bold');
set(gca, 'TickDir', 'out', 'TickLength', [.02 .02],'XMinorTick', 'on', 'YMinorTick', 'on');
