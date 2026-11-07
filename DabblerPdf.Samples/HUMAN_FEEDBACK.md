1. For the 01-basic-text ... everything looks fine.
2. For the 02-text-wrapping ...
   - Single Long Word text wrapping did not work.   
   - Also boxes around text have extra margin at bottom and not enough margin at the top
3. For the 03-lines-and-shapes test ... Lines look pretty good, but they are not vertically aligned with there labels.  That might be by design.
4. For the 04-colors test ... 
   - Color Swatches look good, but labels underneath don't have enough vertical margin.
   - Alternating Row simulation looks fine, but the heading font color for that section ("4.Alternating Row Simulation") is that same color as the previous background fill.  Do we need to reset color after background fill?
5. For the 05-images test ... The "ActualSize" image runs up against the right side of the page.
6. For the 06-table-layout test ... The font color for the table layout is wrong.  Again, it looks like the previous background color bleeds into the font color of the next text.  I can't see the column headings at all.
7. For the 07-multi-page test ... Again, the font color looks like it has adopted the color of the most recent block that has a background color -- the font is way too light.
8. For the 08-padding test ...  everything looks OK, except for the labels outside of the boxes.  It looks like all the labels are shifted up too much.  These seems to be a systematic issue across all samples.
9.for the 09-invoice test ...  the text in the table and below it is too light ... again, it looks like the most recent background color becomes the font color of the next-generated text.
10. for the 10-stress test ... same issue as with the 09-invoice test.