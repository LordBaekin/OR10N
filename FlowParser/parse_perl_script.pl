#!/usr/bin/perl
use strict;
use warnings;
use PPI;

# Check if the file path is passed as an argument
my $file_path = shift or die "Please provide a file path to parse.\n";

# Read the Perl file content
my $document = PPI::Document->new($file_path);

# Extract all subroutines
my $subs = $document->find('PPI::Statement::Sub');

foreach my $sub (@$subs) {
    my $name = $sub->name;
    my $body = $sub->block->content;
    print "Subroutine: $name\n$body\n";
}
