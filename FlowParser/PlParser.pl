use strict;
use warnings;
use PPI;

# Parse a Perl script and return an object tree
sub parse_perl_script {
    my ($file) = @_;
    my $document = PPI::Document->new($file);
    return $document;
}

# Traverse the Perl document to extract subroutines and control structures
sub extract_methods {
    my ($document) = @_;
    my $subs = $document->find('PPI::Statement::Sub');
    
    my @nodes;
    foreach my $sub (@$subs) {
        print "Found subroutine: ", $sub->name, "\n";
        push @nodes, {
            type => "Subroutine",
            name => $sub->name,
            body => $sub->content
        };
    }
    return \@nodes;
}

# Example usage
my $file = 'quest_script.pl';  # Replace with the actual file path
my $document = parse_perl_script($file);
my $nodes = extract_methods($document);
